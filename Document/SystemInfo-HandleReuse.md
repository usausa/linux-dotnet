# 🛠️ LinuxDotNet.SystemInfo ハンドル保持化 作業指示書

- 対象リポジトリ: `lib-LinuxDotNet`（`LinuxDotNet.SystemInfo`）
- 実行環境: Linux 実機（x64 PC と Raspberry Pi（arm64）。両方で実施する）
- 作成日: 2026-10-06
- 関連文書: `lib-MacDotNet/Document/SystemInfo-HandleReuse.md`（Mac 版）

> 📝 **進め方**
> この文書はチェックリスト形式です。作業が終わった項目は `- [ ]` を `- [x]` に更新してください。
> 環境ごとに結果が違う項目は、§📝「結果記録」の表にも記入してください。
> 判断に迷う点や、本書と実際の挙動が食い違う点が見つかった場合は、作業を止めてユーザーに確認してください。

---

## 📌 1. 目的と背景

今の `Update()` は呼ばれるたびに次の処理を行っています。

- `/proc` や `/sys` のファイルを `StreamReader`／`File.ReadAllText` で open → read → close する
- 行ごとに `string` を生成する

利用側（`Service-PrometheusExporter`）は、オブジェクトを一度だけ作ってスクレイプのたびに `Update()` を呼ぶ使い方をしています。

そこで次のように変えて **`Update()` の実行コストを下げる** のが目的です。

- ファイルハンドルは **作成時に開いて保持** する
- 読むたびに **オフセット 0 から pread で読み直す**
- 読んだ内容は **再利用するバイトバッファ** に入れ、**バイト列のまま直接パース** する
- これに合わせて各クラスを **`IDisposable`** にする

---

## 🧭 2. 決定済みの方針

ユーザーとの合意事項です。変更しないでください。

- [x] 内容を理解した

| # | 方針 |
|---|---|
| D1 | **破壊的変更は許容** する |
| D2 | **非スレッドセーフ前提** でよい（共有バッファや保持ハンドルの排他は不要） |
| D3 | 目的は **実行コストの低減**。`StreamReader` を使わず、再オープンせずに読み直せる方式（保持ハンドル＋pread＋バイト列パース）を採用し、実装を統一する |
| D4 | **`Update()` を持つクラスはすべて `IDisposable`** にする。保持するハンドルが実際にはないクラスも、作りを揃えるために `IDisposable`（Dispose は実質何もしない）にする |
| D5 | `PlatformProvider` をファサードとする。コンストラクタは公開せず、**各クラスに `internal static Create(...)` ファクトリを用意** する。オープン失敗時の扱いはファクトリとクラス内部で完結させ、Provider から先（利用側）では意識しなくてよい作りにする |
| D6 | ホットプラグがあり得る処理では、**読み込みに失敗したら一度だけ開き直す** |
| D7 | 作業はユーザーが別途指示する Linux 実機で行う（ログインして検証する。またはユーザーが本書のコマンドを実行する） |

### コーディング規約（`AGENTS.md`）

- メンバ変数に `_` プレフィックスを付けない
- **ビルド警告ゼロ**（net8.0 と net10.0 の両方）
- 警告を抑制する必要が出た場合は、**適用する前にユーザーに確認** する
- 既存ファイルの改行コードは変えない。新規テキストファイルは **CRLF** にする

### Git の運用

- [x] 作業用ブランチ `feature/systeminfo-handle-reuse` を作成した
- コミットは Phase ごとに行う。**push とバージョン番号の変更はユーザーの指示があるまで行わない**

---

## 🎯 3. 対象クラスと方式

方式の凡例:

- **Hold**: `KernelFile` でハンドルを保持し、pread で読み直す
- **Hold(N)**: 子オブジェクトがそれぞれ `KernelFile` を保持する
- **OneShot**: 毎回 open/close するが、バッファは再利用してバイト列でパースする
- **None**: 保持するリソースがない（Dispose は実質何もしない）

| 優先 | クラス | 読むファイル | 方式 | 再オープン(D6) | 備考 |
|---|---|---|---|---|---|
| ◎ | `HardwareMonitor` / `HardwareSensor` | `/sys/class/hwmon/hwmon*/…_input` | Hold(N) | 対象 | センサーの数だけ open していたのを解消 |
| ◎ | `CpuDevice` / `CpuCore` / `CpuPower` | `cpufreq/scaling_cur_freq`、`intel-rapl/energy_uj` | Hold(N) | 対象 | CPU の offline/online で消える。`energy_uj` は root でしか読めないことがある |
| ◎ | `BatteryDevice` | `power_supply/<bat>/` の6ファイル | Hold(6) | 対象 | `Status` は値が変わったときだけ string を作る |
| ○ | `MainsDevice` | `power_supply/<ac>/online` | Hold | 対象 | USB-C の電源は抜き差しで現れたり消えたりする |
| ○ | `SystemStat` | `/proc/stat` | Hold | 共通処理 | 多コア機ではファイルが大きい。`intr` 行がとても長い |
| ○ | `MemoryStat` | `/proc/meminfo` | Hold | 共通処理 | |
| ○ | `VirtualMemoryStat` | `/proc/vmstat` | Hold | 共通処理 | |
| ○ | `DiskStat` | `/proc/diskstats` | Hold | 共通処理 | デバイス名はバイト列で照合する |
| ○ | `NetworkStat` | `/proc/net/dev` | Hold | 共通処理 | IF 名はバイト列で照合する |
| ○ | `TcpStat` | `/proc/net/tcp`、`/proc/net/tcp6` | Hold | 共通処理 | 接続数が多いとファイルが巨大になる（§4.5） |
| ○ | `WirelessStat` | `/proc/net/wireless` | Hold | 共通処理 | |
| ○ | `LoadAverage` / `Uptime` / `FileHandleStat` | `/proc/loadavg`、`/proc/uptime`、`/proc/sys/fs/file-nr` | Hold | 共通処理 | |
| △ | `ProcessSummary` | `/proc/<pid>/status` | OneShot | ― | pid が毎回変わるため保持できない（§4.6） |
| △ | `FileSystemUsage` | `statfs(path)` | None | ― | fd を保持すると umount を妨げるので保持しない |
| ― | `HardwareInfo` / `KernelInfo` / `MountInfo` / `PartitionInfo` / `ProcessInfo` / `UsbDevice` | ― | 対象外 | ― | `Update()` がないスナップショット型。**ファクトリ化（D5）だけ** 行う |

「共通処理」は、`KernelFile` が全クラス共通で持つ「失敗時に1回だけ開き直す」動作をそのまま使うという意味です（§4.2）。

> ℹ️ `lib-RaspberryDotNet`（`Vcio` / `GpioMap`）は、既に fd や mmap を保持する `IDisposable` 実装になっているので、今回は変更しません。
> Raspberry Pi は **LinuxDotNet の arm64 検証環境** として使います。

---

## 🏗️ 4. 設計仕様

### 4.1 クラスの基本形（D4/D5）

```csharp
public sealed class MemoryStat : IDisposable
{
    private readonly KernelFile file;

    private bool disposed;

    public DateTime UpdateAt { get; private set; }
    // ... プロパティは既存どおり

    private MemoryStat(KernelFile file)
    {
        this.file = file;
    }

    internal static MemoryStat Create()
    {
        var instance = new MemoryStat(new KernelFile("/proc/meminfo"));
        instance.Update();   // 失敗しても例外にせず、インスタンスを返す
        return instance;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        file.Dispose();
    }

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!file.Read())
        {
            return false;
        }

        // file.Content (ReadOnlySpan<byte>) を直接パースする

        UpdateAt = DateTime.Now;
        return true;
    }
}
```

規則:

- [x] コンストラクタは `private` にする。`PlatformProvider.GetXxx()` からは `Xxx.Create()` を呼ぶ
- [x] **ファクトリはオープン失敗で例外を投げない**。常に null でないインスタンスを返し、失敗は `Update()` の戻り値 `false` で表す
  - 今の `SystemStat` などは、ファイルがないと例外になる。この挙動を変更する
- [x] `Dispose()` は何度呼んでもよい（冪等）。Dispose 後に `Update()` を呼んだら `ObjectDisposedException` を投げる
- [x] ファイナライザは実装しない（`SafeFileHandle` 側が持っている）
- [x] 保持するリソースがないクラス（None/OneShot）も同じ形にする。`Dispose()` では `disposed = true` だけを行う
- [x] スナップショット型（§3 の対象外の行）は `IDisposable` にしない。ファクトリ化だけ行う

### 4.2 共通ヘルパー `KernelFile`（新規 `KernelFile.cs`）

```csharp
namespace LinuxDotNet.SystemInfo;

using Microsoft.Win32.SafeHandles;

internal sealed class KernelFile : IDisposable
{
    private readonly string path;

    private readonly bool singleRead;

    private SafeFileHandle? handle;

    private byte[] buffer;

    private int length;

    public KernelFile(string path, int bufferSize = 4096, bool singleRead = false)
    {
        this.path = path;
        this.singleRead = singleRead;
        buffer = new byte[bufferSize];
    }

    public string Path => path;

    public bool IsOpen => handle is not null;

    public ReadOnlySpan<byte> Content => buffer.AsSpan(0, length);

    public void Dispose() => Close();

    public void Close()
    {
        handle?.Dispose();
        handle = null;
        length = 0;
    }

    // 先頭から読み直す。保持中のハンドルで失敗したら、一度だけ開き直して再試行する (D6)
    public bool Read()
    {
        if (handle is null)
        {
            return Open() && ReadCore();
        }

        if (ReadCore())
        {
            return true;
        }

        Close();
        return Open() && ReadCore();
    }

    private bool Open()
    {
        try
        {
            handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private bool ReadCore()
    {
        try
        {
            var total = 0;
            while (true)
            {
                if (total == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                var read = RandomAccess.Read(handle!, buffer.AsSpan(total), total);
                if (read == 0)
                {
                    break;
                }

                total += read;

                // sysfs の単一値ファイルは1回の読み込みで全体が返るため、EOF 確認の pread を省く (Phase 0 で確認してから有効化)
                if (singleRead && (total < buffer.Length))
                {
                    break;
                }
            }

            length = total;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            length = 0;
            return false;
        }
    }
}
```

要点:

- `RandomAccess.Read(handle, span, offset)` は pread です。procfs/sysfs（seq_file/kernfs）は **オフセット 0 から読むと内容を作り直す** ので、ハンドルを保持したまま最新値が読めます（Phase 0 で確認します）。
- `FileStream` を Seek して `StreamReader` を使う方式は使いません。内部バッファに古い内容が残る問題と、`Length=0` のファイルの扱いの問題があるためです。
- `FileShare.ReadWrite` を指定します。Unix の .NET は FileShare の指定によって flock を取ることがあるので、それを避けます。
- `RandomAccess` が procfs のハンドルで `NotSupportedException` を投げる場合は、`pread` を `LibraryImport` で直接呼ぶ形に切り替えます（Phase 0 の L0-3 で判定します）。
- 値が1つだけの sysfs ファイル（`HardwareSensor`、`CpuCore`、`CpuPower`、`BatteryDevice`、`MainsDevice`）は、`bufferSize: 64` 程度にします。`singleRead: true` は **L0-2 で安全と確認できた場合だけ** 使います。

### 4.3 パースヘルパー（新規 `KernelFileParser.cs`、`internal static`）

既存の `FileHelper`（string ベース）は、スナップショット型でだけ使い続けます。`Update()` の経路では使いません。

| メソッド | 内容 |
|---|---|
| `bool TryReadLine(ref ReadOnlySpan<byte> remaining, out ReadOnlySpan<byte> line)` | `\n` で1行ずつ取り出す |
| `ReadOnlySpan<byte> NextToken(ref ReadOnlySpan<byte> span)` | 空白とタブを飛ばして、次のトークンを返す |
| `ulong ParseUInt64(ReadOnlySpan<byte>)` / `long ParseInt64` / `int ParseInt32` | 手書きの数字ループで実装する（失敗時は 0。今の `TryParse ? v : 0` と同じ挙動） |
| `ulong ParseHex(ReadOnlySpan<byte>)` | `TcpStat` の状態列用 |
| `double ParseDouble(ReadOnlySpan<byte>)` | `System.Buffers.Text.Utf8Parser.TryParse` を使う（loadavg、uptime） |
| `ReadOnlySpan<byte> TrimEnd(ReadOnlySpan<byte>)` | 末尾の `\n` と空白を除く |

- キーの照合は `"MemTotal:"u8` のような UTF-8 リテラルと `SequenceEqual` / `StartsWith` で行います。
- `string` は **新しいエントリ（CPU 名、デバイス名、IF 名）を初めて見たとき** と **`BatteryDevice.Status` の値が変わったとき** だけ生成します。既存エントリとの照合は、エントリが `byte[]` で持つ名前と比べます。
- `stackalloc Range[]` と `MemoryExtensions.Split` は char 版しかありません（net8.0）。バイト列では上記の `NextToken` を使います。

### 4.4 子オブジェクトの所有権

- `CpuCore`、`CpuPower`、`HardwareSensor` は `KernelFile` を持ちますが、**公開の `IDisposable` にはしません**。`internal void Close()` を持たせます。
- 親（`CpuDevice`、`HardwareMonitor`）の `Dispose()` が子の `Close()` を呼びます。
- 子の `Update()` は、親が Dispose された後に呼ばれたら `ObjectDisposedException` を投げます。
- `PlatformProvider.GetHardwareMonitors()` の戻り値は `IReadOnlyList<HardwareMonitor>` のままです。**利用側が要素をそれぞれ Dispose** します。

### 4.5 `TcpStat` の大きなファイル

接続数が多いと `/proc/net/tcp` は数 MB になり、`KernelFile` のバッファがその大きさのまま残ります。

- [x] Phase 1 の計測で、実環境のサイズ（`wc -c /proc/net/tcp*`）を記録する
- 1MB を超えるケースが現実的にあり得る場合は、`TcpStat` だけ **固定 64KB バッファのチャンク処理**（最後の不完全な行をバッファの先頭へ詰め直す）にしてよいです。その場合は結果記録にその旨を書いてください。

### 4.6 `ProcessSummary`（OneShot）

- pid ディレクトリの列挙は `FileSystemEnumerable<int>` を使い、ファイル名の span から pid を直接 parse します。パス文字列を作らずに済みます。
- 各 `/proc/<pid>/status` は、`File.OpenHandle` → `RandomAccess.Read`（インスタンスが持つ再利用バッファへ）→ Dispose の順で読みます。`StreamReader` は使いません。
- **オプション（計測して効果がある場合だけ採用）**: `status` ではなく `/proc/<pid>/stat` の第20フィールド（num_threads）を読む。`stat` のほうがカーネル側で内容を作るコストが小さいためです。comm にスペースや `)` が含まれ得るので、**最後の `)` より後ろ** を数えます。

---

## 🖥️ 5. 検証環境の準備

各環境（x64 と Raspberry Pi）で実施します。

- [x] E-1 環境情報を記録した（§📝 結果記録の「環境」表）

```bash
uname -a
```

```bash
cat /etc/os-release
```

```bash
nproc && free -h
```

```bash
dotnet --info
```

- [x] E-2 .NET SDK 10 と net8.0 ランタイムが入っている（両方の TFM をテストするため）
- [x] E-3 `python3` と `strace` が使える（`sudo apt install -y strace python3` など）
- [x] E-4 リポジトリを clone して、`dotnet build -c Release` が **警告ゼロ** で通ることを確認した（変更前の状態で）
- [x] E-5 作業ディレクトリ `~/handle-reuse/` を作成した（変更前後のバイナリと結果を置く）

---

## 🔬 Phase 0: 前提確認（PoC）

ライブラリには手を入れず、方式が成り立つかを確認します。

### L0-1 / L0-2: pread で読み直せるか、1回の読み込みで全体が返るか（Python）

`~/handle-reuse/pread_check.py` として保存して実行します。

```python
#!/usr/bin/env python3
import glob, os, time

def targets():
    t = ["/proc/stat", "/proc/meminfo", "/proc/vmstat", "/proc/loadavg", "/proc/uptime",
         "/proc/diskstats", "/proc/net/dev", "/proc/net/tcp", "/proc/net/tcp6",
         "/proc/net/wireless", "/proc/sys/fs/file-nr"]
    t += sorted(glob.glob("/sys/devices/system/cpu/cpu[0-9]*/cpufreq/scaling_cur_freq"))[:2]
    t += sorted(glob.glob("/sys/class/hwmon/hwmon*/*_input"))[:6]
    t += sorted(glob.glob("/sys/class/power_supply/*/capacity"))
    t += sorted(glob.glob("/sys/class/power_supply/*/online"))
    t += sorted(glob.glob("/sys/class/powercap/intel-rapl:0/energy_uj"))
    return [p for p in t if os.path.exists(p)]

def read_all(fd, size):
    chunks, off = [], 0
    while True:
        b = os.pread(fd, size, off)
        chunks.append(len(b))
        if not b:
            break
        off += len(b)
    return off, chunks

for path in targets():
    try:
        fd = os.open(path, os.O_RDONLY)
    except OSError as e:
        print(f"SKIP {path}: {e}")
        continue
    try:
        first = os.pread(fd, 65536, 0)
        time.sleep(1.0)
        second = os.pread(fd, 65536, 0)
        total, chunks = read_all(fd, 65536)
        state = "changed" if first != second else "same"
        single = "single-ok" if len(first) == total else "multi-chunk"
        print(f"OK   {path}: {state}, first={len(first)}, total={total}, {single}, chunks={chunks[:6]}")
    except OSError as e:
        print(f"ERR  {path}: {e}")
    finally:
        os.close(fd)
```

```bash
python3 ~/handle-reuse/pread_check.py
```

- [x] L0-1 `/proc/uptime`、`/proc/stat` などの値が動くファイルが `changed` になった（ハンドルを保持したまま読み直せる）
- [x] L0-2 sysfs の単一値ファイルが、すべて `single-ok` になった。→ `singleRead: true` を採用してよいかを判定し、結果記録に書く
- [x] L0-2b `/proc` の大きなファイルが `multi-chunk` になるかを記録した（ループして読む必要があるかの確認）
- [x] 出力を結果記録に貼り付けた

### L0-3: .NET の `RandomAccess.Read` が procfs で動くか

`__Sandbox/WorkSystemInfoMonitor`（Phase 1 で作るツール）に `poc` コマンドを先に作り、net8.0 と net10.0 で実行します。

```csharp
// poc: /proc/uptime と hwmon の1ファイルについて、File.OpenHandle したハンドルを保持したまま
// RandomAccess.Read(handle, buf, 0) を 1 秒間隔で 5 回読み、内容を表示する。
// RandomAccess.GetLength(handle) の値と、例外が出たかどうかも表示する。
```

- [x] L0-3 net10.0 で値が変わることを確認した
- [x] L0-3 net8.0 で値が変わることを確認した
- [x] L0-3 `NotSupportedException` などが出た場合は、pread を P/Invoke する方式に切り替えると決めた（出なければ不要）

### L0-4: CPU の offline/online で保持ハンドルがどうなるか（再オープン方針の確認）

```bash
python3 - <<'EOF'
import os, subprocess
p = "/sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq"
fd = os.open(p, os.O_RDONLY)
print("before   ", os.pread(fd, 64, 0))
subprocess.run(["sudo", "sh", "-c", "echo 0 > /sys/devices/system/cpu/cpu1/online"])
try:
    print("offline  ", os.pread(fd, 64, 0))
except OSError as e:
    print("offline  ERR", e)
subprocess.run(["sudo", "sh", "-c", "echo 1 > /sys/devices/system/cpu/cpu1/online"])
try:
    print("online   ", os.pread(fd, 64, 0))
except OSError as e:
    print("online   ERR", e)
fd2 = os.open(p, os.O_RDONLY)
print("reopen   ", os.pread(fd2, 64, 0))
EOF
```

- [x] L0-4 online に戻した後、**古い fd がエラーになるか、値を返すか** を記録した（エラーになるなら、D6 の再オープンが必要だという根拠になる）
- [x] L0-4 再オープンした fd では正しい値が読めることを確認した

> ⚠️ cpu0 は offline にできないことが多いので cpu1 を使います。テストの最後に必ず online に戻してください。

- [x] **Phase 0 の結論** を結果記録に書いた（方式を採用できるか、`singleRead` を使うか、pread を P/Invoke にするか）

---

## 📏 Phase 1: ベースライン計測（変更前）

**ライブラリを変更する前に** 実施します。ここで作るツールは、変更前と変更後のどちらでもコンパイルできるように書きます。
具体的には、オブジェクトは `PlatformProvider.GetXxx()` で取得し、解放は `(x as IDisposable)?.Dispose()` で行います。

### 1-1. ツールの作成

- [x] T-1 `__Sandbox/WorkSystemInfoBenchmark`（BenchmarkDotNet、`[MemoryDiagnoser]`、net10.0）を作成し、`__Sandbox/SandboxLinux.slnx` に追加した
  - `LinuxDotNet.SystemInfo` は ProjectReference で参照する
  - `[GlobalSetup]` で全オブジェクトを取得し、`[GlobalCleanup]` で `(x as IDisposable)?.Dispose()` する
  - ベンチマークはクラスごとに1つ作り、`Update()` を呼ぶ。対象は次のとおり
    - `SystemStat`、`MemoryStat`、`VirtualMemoryStat`、`LoadAverage`、`Uptime`、`FileHandleStat`
    - `DiskStat`、`NetworkStat`、`TcpStat`、`Tcp6Stat`、`WirelessStat`
    - `ProcessSummary`、`CpuDevice`、`BatteryDevice`、`MainsDevice`
    - `HardwareMonitors`（全センサーの Update）、`FileSystemUsage("/")`
    - `All`（上記すべて。Exporter の1回のスクレイプに相当）
- [x] T-2 `__Sandbox/WorkSystemInfoMonitor`（コンソール、net10.0 と net8.0）を作成した。コマンドは次の4つ
  - `poc`: L0-3 の処理
  - `dump`: 全オブジェクトを作成して1回 Update し、全プロパティを `名前=値` の形で出力する（変更前後の比較用）
  - `loop --iterations N --interval MS [--log file.csv] [--verbose]`
    - 全オブジェクトで Update を繰り返す
    - 1回ごとに次の値を CSV に出す: 経過時間、Update の合計所要時間、`GC.GetAllocatedBytesForCurrentThread()` の差分、fd 数（`/proc/self/fd` のエントリ数）、各 Update の戻り値（false の数）
    - `--verbose` のときは `CpuDevice`、`HardwareMonitors`、`BatteryDevice`、`MainsDevice`、`NetworkStat` の値も出す
  - 終了時に Dispose して、fd 数が開始時に戻ったかを表示する

### 1-2. 変更前バイナリの保存

```bash
dotnet publish __Sandbox/WorkSystemInfoMonitor -c Release -f net10.0 -o ~/handle-reuse/before/monitor
```

- [x] B-1 変更前のモニターを `~/handle-reuse/before/monitor` に publish した
- [x] B-2 `dump` を実行して保存した

```bash
dotnet ~/handle-reuse/before/monitor/WorkSystemInfoMonitor.dll dump > ~/handle-reuse/dump-before.txt
```

### 1-3. ベンチマーク

```bash
dotnet run -c Release --project __Sandbox/WorkSystemInfoBenchmark -- --filter '*' --exporters github
```

- Raspberry Pi では時間がかかるので `--job short` を付けてよいです。付けた場合は、変更後の計測でも同じ指定にします。
- [x] B-3 結果（`BenchmarkDotNet.Artifacts/results/*-report-github.md`）を `Document/HandleReuse/results/benchmark-before-<env>.md` にコピーした（`<env>` は `x64` か `rpi`）

### 1-4. syscall 数の計測

起動時の分を差し引くため、反復 0 回と 1000 回で計測します。

```bash
strace -f -c -e trace=openat,close,read,pread64,lseek,newfstatat,statx -o ~/handle-reuse/strace-before-0.txt dotnet ~/handle-reuse/before/monitor/WorkSystemInfoMonitor.dll loop --iterations 0 --interval 0
```

```bash
strace -f -c -e trace=openat,close,read,pread64,lseek,newfstatat,statx -o ~/handle-reuse/strace-before-1000.txt dotnet ~/handle-reuse/before/monitor/WorkSystemInfoMonitor.dll loop --iterations 1000 --interval 0
```

- [x] B-4 (1000回 − 0回) ÷ 1000 で、syscall ごとの「1反復あたりの回数」を出して記録した
- [x] B-5 `wc -c /proc/net/tcp /proc/net/tcp6 /proc/stat` を記録した（§4.5 の判断材料）
- [x] B-6 ここまでをコミットした（ツールだけで、ライブラリは未変更）

---

## 🔧 Phase 2: 実装

上から順に進めます。各ステップで `dotnet build -c Release` の **警告ゼロ** を確認してください。

### 2-1. 基盤

- [x] I-1 `KernelFile.cs` を追加した（§4.2。Phase 0 の結論に合わせて `singleRead` や pread P/Invoke を反映する）
- [x] I-2 `KernelFileParser.cs` を追加した（§4.3）

### 2-2. ◎ sysfs 系（Hold(N)、再オープン対象）

- [x] I-3 `HardwareSensor`（KernelFile を保持し、`internal Close()` を持つ）と `HardwareMonitor`（IDisposable、`internal static GetMonitors()` は維持）
- [x] I-4 `CpuCore`、`CpuPower`、`CpuDevice`（IDisposable、`Create()`）
- [x] I-5 `BatteryDevice`（6つの KernelFile。`Status` は変化したときだけ string を作る。`Supported` は維持）
- [x] I-6 `MainsDevice`

### 2-3. ○ /proc 系（Hold）

- [x] I-7 `SystemStat`
- [x] I-8 `MemoryStat`
- [x] I-9 `VirtualMemoryStat`
- [x] I-10 `DiskStat`
- [x] I-11 `NetworkStat`
- [x] I-12 `TcpStat`（`Create(int? version)`。§4.5 に従う）
- [x] I-13 `WirelessStat`
- [x] I-14 `LoadAverage`、`Uptime`、`FileHandleStat`

### 2-4. △ その他

- [x] I-15 `ProcessSummary`（OneShot。§4.6）
- [x] I-16 `FileSystemUsage`（None。`Create(string path)`）
- [x] I-17 スナップショット型のファクトリ化: `HardwareInfo`、`KernelInfo`（`internal static Create()`）。`MountInfo`、`PartitionInfo`、`ProcessInfo`、`UsbDevice` は既存の static メソッドのままでよい
- [x] I-18 `PlatformProvider` を全部ファクトリ呼び出しに変えた
- [x] I-19 `Update()` の経路から `StreamReader`、`File.ReadAllText`、`FileHelper` の呼び出しがなくなったことを grep で確認した

```bash
grep -n -E "StreamReader|ReadAllText|FileHelper\." LinuxDotNet.SystemInfo/*.cs
```

### 2-5. サンプルとドキュメント

- [x] I-20 `Example.SystemInfo.ConsoleApp` を `using var` などで Dispose する形に直した
- [x] I-21 `README.md` に使用例があれば、Dispose する形に直した
- [x] I-22 `dotnet build -c Release`（ソリューション全体、net8.0 と net10.0）が警告ゼロで通った
- [x] I-23 コミットした

---

## ✅ Phase 3: 正しさの検証

```bash
dotnet publish __Sandbox/WorkSystemInfoMonitor -c Release -f net10.0 -o ~/handle-reuse/after/monitor
```

- [x] C-1 変更後のモニターを `~/handle-reuse/after/monitor` に publish した
- [x] C-2 変更前と変更後の `dump` を続けて取り、diff した

```bash
dotnet ~/handle-reuse/before/monitor/WorkSystemInfoMonitor.dll dump > ~/handle-reuse/dump-before2.txt && dotnet ~/handle-reuse/after/monitor/WorkSystemInfoMonitor.dll dump > ~/handle-reuse/dump-after.txt
```

```bash
diff ~/handle-reuse/dump-before2.txt ~/handle-reuse/dump-after.txt
```

  - 判定基準
    - 静的な値（名前、件数、MemTotal、デバイス一覧など）は **完全に一致** すること
    - カウンタ類は after ≥ before で、差が妥当な範囲であること
- [x] C-3 OS のツールの値と突き合わせた（`cat /proc/meminfo`、`cat /proc/loadavg`、`cat /sys/class/hwmon/*/temp*_input`、`ss -s`、`cat /proc/net/dev` など）
- [x] C-4 `loop --iterations 10 --interval 1000 --verbose` で値が更新され続けることを確認した
- [x] C-5 Dispose の挙動を確認した（2回呼んでも例外にならない。Dispose 後の Update で `ObjectDisposedException` になる。終了時に fd 数が開始時に戻る）
- [ ] C-6 net8.0 でも C-2 と C-4 を実施した（`-f net8.0` で publish する） → 実施しない（2026-10-07 ユーザー指示で、net8.0 での確認は不要）
- [ ] C-7 存在しないパスや権限不足のケースでも例外にならず、`Update()` が false を返すことを確認した（例: `energy_uj` を一般ユーザーで読む、`/proc/net/wireless` がない環境）

---

## 📊 Phase 4: 性能比較（変更後）

- [x] P-1 Phase 1-3 と同じ条件でベンチマークを実行し、`Document/HandleReuse/results/benchmark-after-<env>.md` に保存した
- [x] P-2 Phase 1-4 と同じ条件で strace を計測した（after 側のバイナリで）
- [x] P-3 結果記録の「性能比較」表に記入した
- [x] P-4 判定基準（下記）を確認し、満たさない項目があれば原因を調べて記録した

| 判定基準 | 内容 |
|---|---|
| 悪化なし | 全クラスで、after の Mean が before の **+5% 以内** |
| 割り当て | Hold のクラスは、定常状態の Allocated が **0 B**（新規エントリが出た回は除く）。OneShot は before 比で大きく削減されていること |
| ◎クラス | Mean で **30% 以上改善** を目標とする（未達でも悪化がなければ採用してよい。数値を記録する） |
| syscall | Hold のクラスは、定常状態で1反復あたりの `openat` と `close` が **0回** |

---

## 🔌 Phase 5: ホットプラグと長時間稼働

after のモニターを `loop --interval 1000 --verbose --log` で動かしながら実施します。

- [ ] H-1 **CPU offline/online**（x64 と Pi の両方。cpu1 で行う）
  - offline の間は `CpuCore.Update()` が false を返し、例外にならないこと
  - online に戻した後、**再オープンによって値の取得が再開** すること
  - fd 数が増え続けないこと

```bash
sudo sh -c 'echo 0 > /sys/devices/system/cpu/cpu1/online'
```

```bash
sudo sh -c 'echo 1 > /sys/devices/system/cpu/cpu1/online'
```

- [x] H-2 **ネットワーク IF の追加と削除**（`NetworkStat` の回帰確認）

```bash
sudo ip link add hr-dummy0 type dummy && sudo ip link set hr-dummy0 up
```

```bash
sudo ip link del hr-dummy0
```

- [ ] H-3 （x64 のみ・任意）**hwmon ドライバの再ロード**

```bash
sudo modprobe -r coretemp && sudo modprobe coretemp
```

  - 例外が出ず、fd がリークしないことを確認する
  - ⚠️ `hwmonN` の番号が変わった場合、同じパスで開き直すと **別のセンサーにつながる、または失敗する** ことがあります（既知の制約。§⚠️ 参照）。実際にどうなったかを記録してください
- [ ] H-4 （ノート PC のみ・任意）AC アダプタの抜き差しで、`MainsDevice.Online` と `BatteryDevice.Status` が切り替わる
- [ ] H-5 （任意）USB ストレージの抜き差しで、`DiskStat` にデバイスが追加・削除される
- [x] H-6 **24時間連続稼働**（Pi を推奨）

```bash
nohup dotnet ~/handle-reuse/after/monitor/WorkSystemInfoMonitor.dll loop --iterations 86400 --interval 1000 --log ~/handle-reuse/longrun.csv > ~/handle-reuse/longrun.out 2>&1 &
```

  - fd 数が一定であること
  - プロセスの RSS が増え続けないこと（`ps -o rss= -p <pid>` を数回記録する）
  - `Update()` の失敗数が 0 であること

---

## 📦 Phase 6: 利用側の対応（ライブラリのパッケージ公開後、ユーザーの指示があったら実施）

`Service-PrometheusExporter` は NuGet パッケージを参照しています（今は `LinuxDotNet.SystemInfo` 1.14.0）。新しいバージョンを公開した後に実施します。

- [ ] U-1 `PrometheusExporter.Instrumentation.Linux/LinuxInstrumentation.cs` を `IDisposable` にした。`PlatformProvider.GetXxx()` で取得したオブジェクトをすべてフィールドかリストで持ち、Dispose で解放する（`GetHardwareMonitors()` の要素もすべて）
- [ ] U-2 Instrumentation の Dispose がホスト終了時に呼ばれることを確認した（`RaspberryInstrumentation` が既に `IDisposable` なので、その呼ばれ方に合わせる）
- [ ] U-3 Exporter を実機で動かし、`/metrics` の出力が変更前と同じ系列名・ラベルであることを確認した
- [ ] U-4 ビルド警告ゼロ（CA2000 などが出ていない）

---

## 🏁 完了条件

- [ ] Phase 0〜5 のチェックがすべて完了している（任意の項目は、実施できなかったなら理由を記録してあればよい）
- [ ] x64 と Raspberry Pi の両方で結果記録が埋まっている
- [ ] 判定基準を満たさない項目について、原因と対応方針が書かれている
- [ ] ユーザーに結果を報告した（要約: 改善率、Allocated、syscall 数、問題点）

---

## ⚠️ 既知の制約・注意事項

- **hwmon の番号が変わる場合**: 再オープンは同じパスで行うので、モジュールの再ロードなどで `hwmonN` の番号が変わると、正しく追従できません。今回の範囲では「例外が出ない、リークしない」までを保証します。デバイスを探し直す処理は今後の課題です。
- **BatteryDevice / MainsDevice の探索**: どのデバイスを使うかはファクトリで1回だけ決めます。作成した後に新しく現れた電源は検出しません（今と同じ挙動）。
- **`/proc/<pid>/` の保持**: pid は毎回変わるので保持しません（`ProcessSummary` は OneShot）。→ 2026-10-07 の変更（741f4a8）で、`ProcessSummary` はプロセスごとのファイルを読まなくなった。
- **作成時に開けなかったファイル**（2026-10-07 ユーザー決定。d0bc02c）: 作成時に開けなかったファイルは、`Update()` で開き直しません。一覧（コア、電力、センサー）には含めません。後から現れたもの（無線、権限の変更、新しいセンサー）を使うには、利用側がオブジェクトを作り直します。一度開けたファイルは、消えている間も開き直しを試み続けます（D6、ホットプラグ）。
- **`FileSystemUsage`**: fd を保持すると umount を妨げるので、パス指定の `statfs` のままにします。
- **読み込みの一貫性**: 内容が複数の pread にまたがる場合、ファイル全体が同じ時点の値になる保証はありません。これは今の `StreamReader` でも同じです。
- **スレッド安全性**: 非スレッドセーフです（D2）。同じインスタンスに対して `Update()` を並行して呼ばないでください。

---

## 📝 結果記録

### 環境

| 項目 | x64 | x64 VM | Raspberry Pi |
|---|---|---|---|
| 機種 / CPU | FUJITSU FARQ23001（ホスト名 q739）/ Intel Core i5-8365U | Hyper-V の VM（ホスト名 vm-monitor）/ AMD Ryzen 7 5700G | |
| コア数 | 4コア8スレッド（nproc 8）、メモリ 3.5 GiB | 2 vCPU（nproc 2）、メモリ 3.1 GiB | |
| OS / カーネル | Ubuntu 24.04.5 LTS / 7.0.0-34-generic | Rocky Linux 10.2 / 6.12.0-211.34.1.el10_2.x86_64 | |
| .NET SDK / Runtime | SDK 10.0.401 / Microsoft.NETCore.App 8.0.31 と 10.0.12（`~/.dotnet` に dotnet-install.sh で追加。§判定・メモ） | VM には入れていない。Windows で publish した self-contained の単一ファイル（ランタイム 10.0.12 を含む）を実行する（§判定・メモ） | |

### Phase 0 の結論

| 項目 | x64 | Raspberry Pi |
|---|---|---|
| L0-1 ハンドルを保持したまま読み直せるか | 読み直せる（値が動くファイルはすべて `changed`） | |
| L0-2 sysfs で `singleRead` を使えるか | 使える（2026-10-07 ユーザー確認済み）。1回の読み込みの中では、64 バイトの1回目の pread で全体が返り、次の pread は 0（34 ファイル × 200 回で例外なし）。スクリプトの `multi-chunk` 表示は、2回の読み込みの間に値の桁数が変わったための見かけ上のもの | |
| L0-2b /proc の大きなファイルは multi-chunk か | `/proc/net/tcp6`（10,966 バイト）は 3 チャンクに分かれた。`/proc/stat`（4,586 バイト）は 64KB バッファなら 1 回。ループして読む必要がある | |
| L0-3 `RandomAccess` は使えるか（net8/net10） | net8.0（8.0.31）と net10.0（10.0.12）の両方で使える。例外なし（`NotSupportedException` も出ない）。`GetLength` は procfs 0、sysfs 4096 | |
| L0-4 online 後の古い fd の挙動 | offline の間は、古い fd も新しく開いた fd も読むと EBUSY（Errno 16）。`cpu1/cpufreq` のリンクは offline の間も残る。online に戻すと、古い fd のままで値が読める（再オープンしなくても回復する） | |
| 採用方式の決定 | 方式は採用できる。`KernelFile` は `File.OpenHandle` と `RandomAccess.Read` で作る（pread の P/Invoke は不要）。sysfs の単一値ファイルは `bufferSize: 64` と `singleRead: true`。/proc は EOF までループして読む。D6 の再オープンは指示書どおり入れる（このマシンの CPU hotplug では必須ではないが、害はない） | |

#### Phase 0 の出力（x64）

L0-1 / L0-2（`pread_check.py`、指示書のスクリプトのまま）:

```text
OK   /proc/stat: changed, first=4586, total=4586, single-ok, chunks=[4586, 0]
OK   /proc/meminfo: changed, first=1615, total=1615, single-ok, chunks=[1615, 0]
OK   /proc/vmstat: changed, first=3959, total=3960, multi-chunk, chunks=[3960, 0]
OK   /proc/loadavg: changed, first=27, total=27, single-ok, chunks=[27, 0]
OK   /proc/uptime: changed, first=15, total=15, single-ok, chunks=[15, 0]
OK   /proc/diskstats: changed, first=1510, total=1510, single-ok, chunks=[1510, 0]
OK   /proc/net/dev: changed, first=451, total=451, single-ok, chunks=[451, 0]
OK   /proc/net/tcp: changed, first=1200, total=1200, single-ok, chunks=[1200, 0]
OK   /proc/net/tcp6: changed, first=4045, total=10966, multi-chunk, chunks=[4045, 4082, 2839, 0]
OK   /proc/net/wireless: same, first=242, total=242, single-ok, chunks=[242, 0]
OK   /proc/sys/fs/file-nr: same, first=27, total=27, single-ok, chunks=[27, 0]
OK   /sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq: changed, first=7, total=8, multi-chunk, chunks=[8, 0]
OK   /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: changed, first=8, total=7, multi-chunk, chunks=[7, 0]
OK   /sys/class/hwmon/hwmon1/curr1_input: changed, first=4, total=5, multi-chunk, chunks=[5, 0]
OK   /sys/class/hwmon/hwmon1/in0_input: changed, first=6, total=6, single-ok, chunks=[6, 0]
OK   /sys/class/hwmon/hwmon2/curr1_input: same, first=2, total=2, single-ok, chunks=[2, 0]
OK   /sys/class/hwmon/hwmon2/in0_input: same, first=2, total=2, single-ok, chunks=[2, 0]
OK   /sys/class/hwmon/hwmon3/temp1_input: changed, first=6, total=6, single-ok, chunks=[6, 0]
OK   /sys/class/hwmon/hwmon4/temp1_input: same, first=6, total=6, single-ok, chunks=[6, 0]
OK   /sys/class/power_supply/CMB1/capacity: same, first=3, total=3, single-ok, chunks=[3, 0]
OK   /sys/class/power_supply/AC/online: same, first=2, total=2, single-ok, chunks=[2, 0]
OK   /sys/class/power_supply/ucsi-source-psy-USBC000:001/online: same, first=2, total=2, single-ok, chunks=[2, 0]
SKIP /sys/class/powercap/intel-rapl:0/energy_uj: [Errno 13] Permission denied: '/sys/class/powercap/intel-rapl:0/energy_uj'
```

- `first` と `total` は別々の読み込み（1秒あけて読み直したもの）の長さなので、値の桁数が変わると `multi-chunk` と表示される。chunks が `[N, 0]` なら、1回の pread で全体が返っている。
- L0-2 の補足チェック: 同じ読み込みの中で `pread(64, 0)` が全体を返し、続く `pread(64, 長さ)` が 0 になるかを、各ファイル 200 回確認した。
  - 対象: cpufreq 8、hwmon 11、power_supply 11（一般ユーザー）と、RAPL の `energy_uj` 4（root で読み取りのみ）。
  - 結果: すべて OK（not-single=0）。

L0-3（net10.0、`WorkSystemInfoMonitor poc` の抜粋）:

```text
Framework: .NET 10.0.12
Open /proc/uptime: OK
Length /proc/uptime: 0
Open /sys/class/hwmon/hwmon3/temp1_input: OK
Length /sys/class/hwmon/hwmon3/temp1_input: 4096
#1 /proc/uptime: 15 bytes: "462.12 3177.28"
#1 /sys/class/hwmon/hwmon3/temp1_input: 6 bytes: "38000"
#2 /proc/uptime: 15 bytes: "463.13 3185.30"
#2 /sys/class/hwmon/hwmon3/temp1_input: 6 bytes: "37000"
#5 /proc/uptime: 15 bytes: "466.13 3208.88"
#5 /sys/class/hwmon/hwmon3/temp1_input: 6 bytes: "38000"
#1 /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: 7 bytes: "400000"
#3 /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: 7 bytes: "899683"
```

L0-3（net8.0、`WorkSystemInfoMonitor poc` の抜粋。net10.0 でも同じ結果）:

```text
Framework: .NET 8.0.31
Open /proc/uptime: OK
Length /proc/uptime: 0
Open /sys/class/hwmon/hwmon5/temp1_input: OK
Length /sys/class/hwmon/hwmon5/temp1_input: 4096
Open /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: OK
Length /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: 4096
#1 /proc/uptime: 16 bytes: "1127.68 6483.73"
#1 /sys/class/hwmon/hwmon5/temp1_input: 6 bytes: "53000"
#1 /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: 8 bytes: "2000000"
#2 /proc/uptime: 16 bytes: "1128.68 6490.58"
#2 /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: 7 bytes: "400000"
#3 /sys/class/hwmon/hwmon5/temp1_input: 6 bytes: "55000"
#5 /proc/uptime: 16 bytes: "1131.69 6511.19"
#5 /sys/devices/system/cpu/cpu1/cpufreq/scaling_cur_freq: 8 bytes: "2229866"
```

L0-4（指示書のスクリプトのまま。終わった後、cpu1 が online に戻っていることを確認した）:

```text
before    b'2099876\n'
offline  ERR [Errno 16] Device or resource busy
online    b'1926809\n'
reopen    b'1926809\n'
```

L0-4 の補足（offline の間に新しく開いた場合）:

```text
offline: cpu1/cpufreq exists: True
offline: fresh open OK, read ERR [Errno 16] Device or resource busy
```

既存バグ修正の確認（`WorkSystemInfoMonitor fixcheck`、net10.0）:

```text
# 修正前（main a0dcdb5）
DiskStat: update=True entries 3 -> 3, same instances 0
NetworkStat: update=True entries 2 -> 2, same instances 0
WirelessStat: update=True entries 1 -> 1, same instances 0
# 修正後（作業ブランチ caf5d5e）
DiskStat: update=True entries 3 -> 3, same instances 3
NetworkStat: update=True entries 2 -> 2, same instances 2
WirelessStat: update=True entries 1 -> 1, same instances 1
```

### 性能比較（Mean / Allocated）

| クラス | before x64 | after x64 | 改善率 | before Pi | after Pi | 改善率 |
|---|---|---|---|---|---|---|
| SystemStat | | | | | | |
| MemoryStat | | | | | | |
| VirtualMemoryStat | | | | | | |
| LoadAverage | | | | | | |
| Uptime | | | | | | |
| FileHandleStat | | | | | | |
| DiskStat | | | | | | |
| NetworkStat | | | | | | |
| TcpStat / Tcp6Stat | | | | | | |
| WirelessStat | | | | | | |
| ProcessSummary | | | | | | |
| CpuDevice | | | | | | |
| BatteryDevice | | | | | | |
| MainsDevice | | | | | | |
| HardwareMonitors | | | | | | |
| FileSystemUsage | | | | | | |
| **All** | | | | | | |

x64 VM（`benchmark-before-x64vm.md`。BenchmarkDotNet は `--inProcess` で実行。§判定・メモ「x64 VM での実施」）:

| クラス | before | after | 改善率 |
|---|---|---|---|
| SystemStat | 24.7 µs / 10,368 B | 10.6 µs / 0 B | −57.2% |
| MemoryStat | 28.6 µs / 12,248 B | 11.3 µs / 0 B | −60.7% |
| VirtualMemoryStat | 49.2 µs / 20,289 B | 30.8 µs / 0 B | −37.4% |
| LoadAverage | 16.1 µs / 7,920 B | 2.90 µs / 0 B | −82.0% |
| Uptime | 15.9 µs / 7,912 B | 2.84 µs / 0 B | −82.1% |
| FileHandleStat | 16.9 µs / 7,920 B | 3.03 µs / 0 B | −82.0% |
| DiskStat | 24.8 µs / 9,192 B | 12.1 µs / 0 B | −51.1% |
| NetworkStat | 53.0 µs / 11,193 B | 24.1 µs / 0 B | −54.6% |
| TcpStat / Tcp6Stat | 450 µs / 9,874 B、449 µs / 9,826 B | 288 µs / 2 B、286 µs / 2 B（定常状態では 0 B。下記） | −36.0%、−36.4% |
| WirelessStat | 19.1 µs / 1,224 B（`/proc/net/wireless` がなく、毎回例外になって false） | 18.0 µs / 736 B（同じ） | −5.9% |
| ProcessSummary | 4.55 ms / 1,917,110 B（プロセス約 170） | 3.40 ms / 20,664 B（プロセス約 172） | −25.2%（割り当ては −98.9%） |
| CpuDevice | 4.9 ns / 0 B（N/A。コアの周波数ファイルも RAPL もない） | 2.2 ns / 0 B（N/A） | N/A |
| BatteryDevice | 2.2 ns / 0 B（N/A） | 2.2 ns / 0 B（N/A） | N/A |
| MainsDevice | 0.6 ns / 0 B（N/A） | 0.4 ns / 0 B（N/A） | N/A |
| HardwareMonitors | 0.6 ns / 0 B（N/A。センサーがない） | 0.6 ns / 0 B（N/A） | N/A |
| FileSystemUsage | 1.62 µs / 0 B | 1.72 µs / 0 B | +6.5%（計測のばらつき。下記） |
| **All** | **6.15 ms / 2,025,159 B** | **4.27 ms / 21,545 B** | **−30.5%（割り当ては −98.9%）** |

### syscall 数（1反復あたり）

| syscall | before x64 | after x64 | before Pi | after Pi |
|---|---|---|---|---|
| openat | | | | |
| close | | | | |
| read | | | | |
| pread64 | | | | |
| newfstatat / statx | | | | |

x64 VM（`strace -f -c`、(1000回 − 0回) ÷ 1000。before と after を続けて計測し、どちらもプロセスは 172〜173 個）:

| syscall | before | after |
|---|---|---|
| openat | 184.3（うち失敗 1.0。`/proc/net/wireless`） | 173.5（うち失敗 1.0。`/proc/net/wireless`） |
| close | 183.3 | 172.5 |
| read | 0.25 | 0.01 |
| pread64 | 543.0 | 364.0 |
| newfstatat / statx | 0 | 0 |
| lseek（参考） | 0.49 | 0.004 |
| flock（参考） | 364.0 | 343.0 |

開いたパスごとの 1 反復あたりの openat（`strace -f -e trace=openat,close`、(100 回 − 0 回) ÷ 100）:

| パス | before | after |
|---|---|---|
| `/proc/<pid>/status`（`ProcessSummary`、OneShot） | 172 | 172 |
| `/proc`（`ProcessSummary` の pid の列挙） | 1 | 1 |
| `/proc/net/wireless`（ファイルがなく失敗する。判定の対象外） | 1 | 1 |
| `/proc/stat`、`/proc/meminfo`、`/proc/vmstat`、`/proc/loadavg`、`/proc/uptime`、`/proc/sys/fs/file-nr`、`/proc/diskstats`、`/proc/net/dev`、`/proc/net/tcp`、`/proc/net/tcp6`（Hold） | 各 1（`/proc/meminfo` は 1.24） | 0 |

- **Hold のクラスは、1 反復あたりの openat と close が 0 回**（判定基準を満たす）。残っている openat は、`ProcessSummary`（OneShot）の分と、ファイルがない `/proc/net/wireless` を開き直そうとする分（判定の対象外と決めたもの）だけ。
- before の `/proc/meminfo` が 1.24 回なのは、GC がメモリの負荷を調べるために読む分（0.24 回）が入っているため。after は割り当てが減って GC が少ないので、この分がない。
- **flock**: .NET はファイルを開くと `flock(LOCK_SH|LOCK_NB)` を取り、閉じるときに解除する（`FileShare.ReadWrite` でも取る。1 回開くと 2 回）。Hold のクラスでは開くときだけなので、1 反復あたりは 0 になる。残りの 343 回は `ProcessSummary` の分（172 個 × 2）。
- B-4 で最初に測った before の値（プロセス約 170 個）: openat 182.1（失敗 1.0）、close 181.1、read 0.25、pread64 535.5、newfstatat 0、lseek 0.48。その後プロセスが増えたので、比較には上の続けて測った値を使う。
- 1000 回の loop の失敗数は、before と after のどちらも `WirelessStat`、`BatteryDevice`、`MainsDevice` がそれぞれ 1000（VM にファイルがないため）。ほかは 0。fd 数は 55 → 55。

### ホットプラグ・長時間稼働

| 項目 | 結果 | 備考 |
|---|---|---|
| H-1 CPU offline/online | x64 VM では実施しない | VM には cpufreq がなく、`CpuCore` を確認できないため（§判定・メモ「x64 VM での実施」）。実機で行う |
| H-2 ネットワーク IF | x64 VM: 問題なし | loop（1 秒間隔、25 回）の途中で `hr-dummy0` を追加し、8 秒後に削除した。6〜13 回目だけ `NetworkStat` に `hr-dummy0` が現れ（インターフェースの数は 11 → 12 → 11）、削除後は消えた。例外も `NetworkStat` の失敗もなく、fd 数は 66 のまま（終了時も 56 → 56） |
| H-3 hwmon 再ロード | x64 VM では実施できない | VM に hwmon がないため。実機で行う（任意の項目） |
| H-4 AC アダプタ | x64 VM では実施できない | VM に電源（power_supply）がないため。ノート PC の実機で行う（任意の項目） |
| H-5 USB ストレージ | x64 VM では実施しない | 任意の項目。VM には USB ストレージをつなげない |
| H-6 24時間（fd / RSS / 失敗数） | x64 VM: 満たす（2026-10-07 13:17〜16:01、9,730 回） | ユーザーの指示で、24 時間ではなく 2 時間以上で十分とした（2 時間 43 分で SIGTERM で止めた）。fd 数は 66 で一定（終了時は 56 → 56）。RSS は最初の 10 分で 49.7 → 59.3 MB、その後 2 時間半は 59.3 → 59.45 MB でほぼ横ばい。失敗は VM にファイルがない `WirelessStat`、`BatteryDevice`、`MainsDevice` だけ（N/A）で、ほかは 0。Pi での実施は未定 |

### 判定・メモ

（基準を満たさなかった項目、気づいた点、今後の課題）

#### 作業前の決定事項（2026-10-06）

- **既存バグを先に修正した**（コミット `caf5d5e`）
  - `DiskStat`、`NetworkStat`、`WirelessStat` は、既存エントリを `item.Name == name`（string と `ReadOnlySpan<char>` の `==`）で探していた。これは中身ではなく参照の比較なので常に false になり、`Update()` のたびに全エントリを作り直していた。
  - `StringComparison.Ordinal` で中身を比べる形に直した。Phase 1 の before の計測は、この修正の後の状態で行う。
  - 同じパターン（`Span` / `ReadOnlySpan` の `==` と `!=`）が他にないことを、ビルドした DLL の IL を調べて確認した。LinuxDotNet、MacDotNet、RaspberryDotNet の全プロジェクトで、修正後は 0 件。
- **ツールはコミットしない**: `__Sandbox` は `.gitignore` の `__*` で除外されている。`WorkSystemInfoBenchmark` と `WorkSystemInfoMonitor` はコミットせず、B-6 のコミットにはドキュメントの更新だけを入れる。
- **エラー時の再オープンは、1反復あたりの判定の対象外**: 開けなかったファイルを `Update()` のたびに開き直そうとする分（例: 一般ユーザーでは読めない `energy_uj`、存在しない `/proc/net/wireless`）は、syscall の判定「1反復あたり openat と close が 0回」に含めない。strace の errors 列で、失敗した openat を区別して記録する。
- **実機には SSH で入る**: 接続先は、必要になったときにユーザーが指示する。

#### Phase 0（x64）で見つかった点（2026-10-07）

- **E-2 と E-4 は、SDK とランタイムを追加して解決した（2026-10-07 ユーザー了承）**
  - Microsoft 公式の `dotnet-install.sh` で、`~/.dotnet` に SDK 10.0.401（Windows 側と同じ）と .NET 8 ランタイム 8.0.31 を入れた。sudo は使わず、apt で入っている dotnet には触れていない。
  - 実機でのコマンドは `DOTNET_ROOT=$HOME/.dotnet` と `PATH=$HOME/.dotnet:$PATH` を付けて実行する（シェルの設定ファイルは変更していない）。
  - SDK 10.0.401 では、main（a0dcdb5）がソリューション全体で警告 0、エラー 0 でビルドできた。
  - 以下は追加する前の状況。
- **E-2 を満たさなかった**: net8.0 のランタイムが入っていない。.NET は Ubuntu パッケージの SDK 10.0.112 とランタイム 10.0.12 だけ。
- **E-4 を満たさなかった**: main（a0dcdb5）をソリューション全体でビルドすると、SDK 10.0.112（コンパイラ 5.0）では失敗する。
  - CS0214（`unsafe` なしのポインタ）が 148 件。内訳は LinuxDotNet.Disk 76、Video4Linux2 68、InputEvent 4。
  - CS9248 が 8 件（Example.GameInput.AvaloniaApp）。
  - CS9057 の警告は、ソースジェネレーターが新しいコンパイラ 5.9 を参照しているため。
  - このリポジトリは SDK 10.0.4xx のコンパイラを前提にしている（Windows 側は 10.0.401）。
  - LinuxDotNet.SystemInfo と `WorkSystemInfoMonitor` は、10.0.112 でもコンパイラ警告 0 でビルドできる。
- **SourceLink の警告**: `.git` のないコピーを Release でビルドすると、SourceLink の警告が出る（コードとは無関係）。実機側のコピーは git clone を土台にして、その上にファイルを上書きする。
- **既存バグの修正を実機で確認**: `fixcheck` で、修正前はエントリが Update のたびに作り直され（same instances 0）、修正後はすべて同じインスタンスになることを確認した（§Phase 0 の出力）。

#### x64 VM での実施（2026-10-07）

- **x64 の実機（q739）に接続できないため、ユーザーの指示で VM（192.168.100.50、root）を使って進める**
  - VM には cpufreq、hwmon、power_supply（バッテリーと AC）、RAPL（powercap）、`/proc/net/wireless` がない。そのため `CpuDevice`、`HardwareMonitors`、`BatteryDevice`、`MainsDevice`、`WirelessStat` は、VM では空か `false` になる。これらの確認は実機（x64 PC と Pi）で行う。
  - VM では Docker で監視系のサービス（cadvisor、prometheus、influxdb、grafana）が動いている。計測には、これらの負荷が少し入る。
  - VM の結果は「x64 VM」として、実機の x64 とは別に記録する。
- **VM に .NET は入れない（ユーザーの希望）**: Windows で linux-x64 向けの self-contained の単一ファイルとして publish し、VM に転送して実行する。
  - `dotnet publish __Sandbox/WorkSystemInfoMonitor -c Release -f net10.0 -r linux-x64 --self-contained -p:PublishSingleFile=true -p:DebugType=embedded -o <出力先>`
  - 指示書の `dotnet ~/handle-reuse/before/monitor/WorkSystemInfoMonitor.dll ...` は、`~/handle-reuse/before/monitor/WorkSystemInfoMonitor ...` として実行する。
  - E-2（SDK と net8.0 ランタイム）と E-4（実機でのビルド）は、この方法では要らない。ビルドは Windows で行い、警告 0 を確認する。
- **ベンチマークは `--inProcess` で実行する**: BenchmarkDotNet は通常、計測のたびにその場でプロジェクトをビルドするので、SDK が要る。
  - そこで `WorkSystemInfoBenchmark` も self-contained で publish し（単一ファイルにはしない）、`WorkSystemInfoBenchmark --filter '*' --inProcess --exporters github` で実行する（InProcessEmitToolchain）。
  - 変更前と変更後を同じ方式で測るので、両者の比較はできる。
- **net8.0 での確認は不要とする（2026-10-07 ユーザー指示）**: C-6 は実施しない。ビルド警告ゼロの確認は、net8.0 も続ける（AGENTS.md）。
- **strace と python3 は、VM に最初から入っていた**（strace のインストールはユーザーが了承していたが、不要だった）。
- **H-1（CPU の offline/online）は VM では行わない**: cpufreq がないので `CpuCore` を確認できず、VM で動いているサービスにも影響するため。実機で行う。
- **Phase 0 の補足確認（VM）**: `pread_check.py` で、/proc のファイルはすべて `changed` または `same` で、1回の pread で全体が返った（`/proc/vmstat` は 4,095 バイトで、`wc -c` と一致）。`WorkSystemInfoMonitor poc`（net10.0）でも、`/proc/uptime` の値が保持したハンドルのまま更新された。sysfs の対象ファイルは VM にはない。
- **B-5（VM）**: `/proc/net/tcp` 1,050 バイト、`/proc/net/tcp6` 1,029 バイト、`/proc/stat` 1,250 バイト。x64 実機の Phase 0 では `/proc/net/tcp6` が 10,966 バイトだった。1MB を超えることは現実的にないので、`TcpStat` はチャンク処理にしない（§4.5）。

#### Phase 2 の実装メモ（2026-10-07）

- 実装は Opus のサブエージェントが行い、差分を監査した。指示書の §4 のとおりで、修正が必要な点はなかった。
- **ビルド**: `dotnet build LinuxDotNet.slnx -c Release --no-incremental` は、net8.0 と net10.0 の両方で警告 0、エラー 0。警告の抑止は追加していない。
- **I-19 の grep の結果**: `Update()` の経路にはない。残っているのは次の 3 種類だけ。
  - 作成時の処理: `BatteryDevice.FindBattery`、`MainsDevice.FindAdapter`、`CpuDevice.AddCpuPower`、`HardwareMonitor.GetMonitors`
  - `FileHelper` 自体
  - スナップショット型: `HardwareInfo`、`KernelInfo`、`MountInfo`、`PartitionInfo`、`ProcessInfo`、`UsbDevice`
- **子クラス（`CpuCore`、`CpuPower`、`HardwareSensor`）の持ち方**
  - 親のファクトリが `KernelFile` を作り、子の internal コンストラクタに渡す。子は `internal Close()` を持つ。
  - 子が自分で `KernelFile` を作ると CA1001 が出ることを、事前に確かめた。この形なら CA1001、CA2000、CA2213 は出ない。
- **§4 との小さな違い**
  - `KernelFile.Path` は自動プロパティにした（IDE0032 のため）。
  - `ParseUInt64` は先頭の `+` を受け付け、`-` があれば 0 を返す（`UInt64.TryParse` と同じ結果）。
  - `DiskStat` と `NetworkStat` は、カウンタ列を `stackalloc` した `Span<ulong>` に読む private メソッドを持つ。
  - `WirelessStat.Status` は、`Int32.TryParse(HexNumber)` と同じ結果になるようにした。
  - `CpuDevice` の Cores と Powers は配列で持つ（`Update()` の foreach でアロケーションしない）。
  - ファクトリの中のディレクトリの列挙では、`IOException` と `UnauthorizedAccessException` を捕捉する。`/sys/class/hwmon` がなければ空のリストを返す。
  - `ProcessSummary` は `FileSystemEnumerable<int>` で pid を列挙し、`File.OpenHandle` と `RandomAccess.Read` でインスタンスのバッファに読む。パス文字列は pid ごとに作る。§4.6 のオプション（`/proc/<pid>/stat` を読む方式）は実装していない。
- **以前の実装との挙動の違い**: 実際のカーネルの出力では起きない。
  - 区切りとして空白とタブの両方を受け付ける。
  - sysfs の値は末尾だけを Trim する。そのため、先頭に空白があると 0 になる。
  - TCP の状態は 16 進の数値で比べる。
  - CPU 名はバイト列の完全一致で照合する。以前は大文字小文字を区別しない比較だった。
  - ファイルがないときは、例外ではなく false や空の結果になる（D5 の意図どおり）。
- **Windows での事前確認（サブエージェント）**: 偽の /proc と /sys を用意し、以前のソースと新しいソースで全公開プロパティを比べた。net8.0 と net10.0 で、すべて一致した。
  - パーサーは、境界値と乱数 2 万件で `TryParse` と一致した。
  - Hold のクラスは、`Update()` 100 回の割り当てが 0 B だった。
  - 実際の procfs と sysfs での挙動は、Phase 3 で確かめる。
- **§4.2 の flock の説明は不正確**
  - .NET は Unix で読み取り用に開くとき、`FileShare.ReadWrite` でも `flock(LOCK_SH|LOCK_NB)` を取る。`FileShare.None` なら `LOCK_EX` になる。
  - 影響は開くときだけで、読み直しのたびに起きるわけではない（以前の実装も同じ）。Phase 4 の strace で確かめる。
- **README の既存の誤り（2026-10-07 ユーザーの指示で修正した。コミット 1682608）**
  - SystemInfo の使用例を、それぞれメソッドに入れてコンパイルして調べた。
  - 存在しないメンバーは `KernelInfo.MaxProcessCount`、`MaxFileCount`、`MaxFileCountPerProcess`、`Uptime.Uptime`、`SystemStat.ProcessRunning`、`ProcessBlocked`、`VirtualMemoryStat.PageFault`、`MajorPageFault` だった。
  - ほかに、`ulong` の値を `Enumerable.Sum` で合計していた（`ulong` のオーバーロードはないのでコンパイルできない。`CpuTotal` を使う形にした）。また、未定義の `IncludeVirtual` を渡していた。
  - 直した後は、すべての例がコンパイルできる。

#### Phase 3 の結果（x64 VM、2026-10-07）

- **C-1**: 変更後のモニターを、Windows で self-contained の単一ファイルとして publish し、VM の `~/handle-reuse/after/monitor` に置いた。
- **C-2（dump の diff）**: 変更前と変更後の dump を続けて取った。
  - 項目はどちらも 490 個で、片方にしかない項目はない。
  - 値が違ったのは 55 項目で、どれも変わって当然のものだった（`UpdateAt`、メモリや空き容量の現在値、`Uptime`、カウンタ）。
  - 名前、件数、`MemTotal`、デバイスとインターフェースの一覧などの静的な値は、完全に一致した。
  - 値が変わったカウンタ 23 個（CPU の tick、`ContextSwitch`、`Interrupt`、`Forks`、`SoftIrq`、ネットワークのバイト数とパケット数、`PageFaults`）は、すべて「後 ≥ 前」だった。
- **C-3（OS のツールとの突き合わせ）**: 変更後の dump の直後に、OS のファイルとコマンドで値を取った。
  - `/proc/meminfo`（MemTotal、MemAvailable、MemFree、Buffers、Swap）、`/proc/loadavg`、`/proc/sys/fs/file-nr` は完全に一致した。
  - `/proc/stat`（cpu の行、ctxt、processes、procs_running）、`/proc/vmstat`、`/proc/diskstats`（sda）、`/proc/net/dev`（eth0）、`/proc/uptime` は、取った時刻のずれの分だけ違い、それ以外は一致した。
  - TCP の件数は `/proc/net/tcp` と `/proc/net/tcp6` の行数（6 と 5、ESTABLISHED 1）と一致した。
  - プロセス数とスレッド数は `ls /proc` と `ps -eLf` に近い値だった（200 と 202、389 と 384。プロセスは常に増減している）。
  - `FileSystemUsage("/")` は `df -B1 /` と `stat -f /` と一致した。
- **C-4（loop --iterations 10 --interval 1000 --verbose）**
  - 値は毎回更新された（eth0 の RxBytes と TxBytes が増え続けた）。
  - 失敗は、VM にファイルがない `WirelessStat`、`BatteryDevice`、`MainsDevice` だけ。
  - 1 回の割り当ては約 25 KB（before の loop は約 2 MB）。fd 数は 55 → 55。
- **C-5（Dispose）**: モニターに `disposecheck` コマンドを追加して確かめた（ツールなのでコミットしない）。
  - 16 個のオブジェクトすべてで、`Dispose()` を 2 回呼んでも例外にならず、Dispose 後の `Update()` は `ObjectDisposedException` になった。
  - VM には CPU のコア（cpufreq）、RAPL、hwmon がないので、子（`CpuCore`、`CpuPower`、`HardwareSensor`）の確認は実機で行う。
  - fd 数は、作成前 39 → 作成後 49 → すべて Dispose した後 39 で、元に戻った。ライブラリが開いた 10 個（`/proc/stat` から `/proc/net/tcp6` まで）は、すべて閉じた。
  - 最初は fd 数が戻らなかった。これは、最初に Console に書いたときに .NET が開く fd（標準出力の複製とシグナル用のパイプ）と、初めて読み込むアセンブリのために開く実行ファイルの fd が、数に入っていたため。Console を先に使い、1 回目を出力なしで実行してから数える形にした。
- **C-6**: 実施しない（2026-10-07 ユーザー指示。net8.0 での確認は不要）。
- **C-7**: VM では、ファイルがないケースだけ確かめられた。`/proc/net/wireless` がなくても、電源のファイルがなくても、例外にならずに `Update()` が false を返した。権限不足のケース（一般ユーザーで `energy_uj` を読む）は VM にファイルがないので、実機で確かめる。

#### Phase 4 の判定（x64 VM、2026-10-07）

| 判定基準 | 結果 |
|---|---|
| 悪化なし（+5% 以内） | 満たす。最初の計測では `FileSystemUsage` が +6.5%（1.62 → 1.72 µs）だったが、before と after を交互に 2 回ずつ測り直すと、before 1.735 / 1.616 µs、after 1.628 / 1.642 µs で、差は計測のばらつきの範囲だった。このクラスは None で、変更は Dispose 後の確認 1 行だけ。`HardwareMonitors` の +4.8% は、VM ではセンサーがなく何もしない呼び出し（1 ns 未満）の差 |
| 割り当て（Hold は 0 B） | 満たす。BenchmarkDotNet では `TcpStat` と `Tcp6Stat` が 2 B と出たが、モニターに追加した `alloccheck`（ウォームアップの後、`Update()` 2,000 回の `GC.GetAllocatedBytesForCurrentThread()` の差）では、Hold のクラスはすべて 0 バイトだった。OneShot の `ProcessSummary` は 1,917,110 B → 20,664 B（−98.9%） |
| ◎クラス（30% 以上改善） | VM では判定できない（N/A）。`HardwareMonitors`、`CpuDevice`、`BatteryDevice` の対象のファイルが VM にないため。実機で計測する |
| syscall（Hold は openat と close が 0 回） | 満たす（§syscall 数の x64 VM の表） |

- **全体**: 1 回のスクレイプに相当する All は、6.15 ms → 4.27 ms（−30.5%）、割り当ては 2,025,159 B → 21,545 B（−98.9%）。
- **Hold の /proc のクラス**: −37〜−82%。値が 1 つだけの小さなファイル（`LoadAverage`、`Uptime`、`FileHandleStat`）ほど改善の割合が大きい（open と close と string の生成がなくなるため）。
- **`TcpStat`（−36%）**: 残りの約 290 µs はカーネルが `/proc/net/tcp` を作る時間。接続が 6 件でも、カーネルは TCP のハッシュ表全体をたどるため。
- **`ProcessSummary`（−25%）**: プロセスごとに開いて閉じる（OneShot）ので、効果は小さい。1 プロセスあたり openat、fstat、flock 2 回、pread 2 回、close がかかる。
- **`WirelessStat`**: `/proc/net/wireless` がない環境では、今も `Update()` のたびに `File.OpenHandle` の中で `FileNotFoundException` が発生して捕捉される（約 18 µs、736〜984 B）。指示書どおりの動作（開けないファイルは毎回開き直す）で、判定の対象外と決めたもの。
- **後で検討する課題**
  - 開けないファイルを毎回開き直すときに、例外のコストがかかる（`WirelessStat` など）。たとえば、開く前に存在を確かめれば、例外を避けられる。
  - `ProcessSummary` の 1 プロセスあたりの syscall。open、pread、close を直接呼べば、flock と fstat と SafeFileHandle の割り当てがなくなる。§4.6 の `/proc/<pid>/stat` を読む方式もある。

#### 検討事項の結論（2026-10-07 ユーザー決定）

**ProcessSummary を「pid の数 ＋ /proc/loadavg」にした（コミット 741f4a8。§4.6 の OneShot から変更）**

- **他のツールの調べ方**（サブエージェントによる調査。出典は各プロジェクトのソース）
  - 全 pid の `stat`（20 番目のフィールド）を合計する: procps-ng（top、ps）、htop、btop、Telegraf、node_exporter の processes collector。node_exporter は、この collector を重いので既定で無効にしている。
  - 全 pid の `status`（`Threads:`）を合計する: psutil（glances）、collectd。以前のライブラリと同じ方式。
  - `/proc/loadavg` の 4 番目のフィールド（`/` の後ろ）を使う: sysstat の `sar -q`（`plist-sz`）、Netdata の `system.active_processes`。
- **カーネルの値の意味**
  - `/proc/loadavg` の 4 番目のフィールドの分母は `nr_threads` で、pid が 0 でないすべてのタスク（カーネルスレッドと、まだ回収されていないゾンビを含む）の数。全 pid の `Threads:` の合計と同じ意味になる。
  - `sysinfo().procs` も同じ値だが、16 ビットなので 65,536 を超えると一周する。
- **VM での計測**（プロセス 175 個）

| 方式 | 時間 | 割り当て | プロセス数 / スレッド数 |
|---|---|---|---|
| 全 pid の status（以前の方式） | 3,265 µs | 21.4 KB | 175 / 370 |
| 全 pid の stat（`File.OpenHandle`） | 2,324 µs | 20.8 KB | 175 / 370 |
| 全 pid の stat（`openat` を直接呼ぶ。flock なし） | 1,884 µs | 216 B | 175 / 370 |
| pid の数 ＋ `/proc/loadavg` | 151 µs | 280 B | 175 / 370 |

- **変更後**: `ProcessCount` は `/proc` の数字のディレクトリの数（プロセスごとには開かない）。`ThreadCount` は、保持した `/proc/loadavg` の 4 番目のフィールドの分母。
  - VM の確認: `ProcessCount` は `ls /proc` と一致した（178）。`ThreadCount` は 373 で、モニター自身が動いている間の値なので、そのスレッド（9）を含む。モニターが終わった後に OS のコマンドで見ると 364。
  - `alloccheck`: 1 回 209 µs、208 B（以前は 3,265 µs、21.4 KB）。
- **注意**: pid 名前空間が別のコンテナの中や、`hidepid` の環境では、`ProcessCount` は見えるプロセスだけ、`ThreadCount` はシステム全体の値になり、範囲が食い違う。ホストでサービスとして動かす使い方なら問題ない。

**開けないファイルの扱い（D5 の補足）**

- **計測（VM）**: ないファイルを開くと、`File.OpenHandle` の例外で 18.2 µs と 984 B かかる。`File.Exists` で調べれば 5.0 µs、P/Invoke の `open` なら 5.4 µs で、どちらも割り当ては 0。
- **ユーザーの決定**
  - 必ず存在する概念（`PlatformProvider` が返す 1 つのオブジェクト）は、今までどおり常にインスタンスを返す。
  - 一覧の要素は、作成時に失敗したものを含めない。
  - 作成時に開けなかったファイルは、後の `Update()` で開き直さない。後から現れたものを使うには、利用側がオブジェクトを作り直す。
  - 一度開けたファイルは、D6 のとおり開き直しを続ける（ホットプラグ）。
- 実装は別のコミットで行う。

**開けないファイルの扱いの実装（コミット d0bc02c。実装は Opus のサブエージェントが行い、監査した）**

- `KernelFile`: 最初の `Read()`（作成時にファクトリが呼ぶ）で開けなかったファイルは、以後 `Read()` で開かずに、すぐに false を返す（syscall も例外もない）。一度開けたファイルは、`Opened` が true になり、今までどおり D6 で開き直す。
- `CpuDevice`（コアと電力）と `HardwareMonitor`（センサー）は、作成時に開けなかったファイルの要素を一覧に入れない。オフラインのコアはファイルを開けるので、一覧に残る。
- 1 つのオブジェクトを返すクラス（`WirelessStat`、`TcpStat`、`BatteryDevice` など）は、コードを変えずに `KernelFile` の規則に従う。
- README に、一覧に含まれない要素と、作り直しが必要なことを追記した。
- **VM での確認**
  - `WirelessStat`（`/proc/net/wireless` がない）は、1 回 0.04 µs、0 B になった（以前は約 18 µs、736〜984 B）。
  - strace で見ると、`/proc/net/wireless` を開こうとするのは作成時の 1 回だけで、反復を 0 回でも 100 回でも同じ。1 反復あたりの openat は 1.0 回まで下がった（`ProcessSummary` が `/proc` を列挙する分だけ）。失敗する openat は、反復しても増えない。
  - loop の 1 回あたりの割り当ては 208 B（H-6 のときは 21.5 KB）。dump の項目は 491 行で、変更前と同じ構成。
  - VM にはコア（cpufreq）、RAPL、hwmon がないので、一覧から外れる動きは実機で確かめる。
