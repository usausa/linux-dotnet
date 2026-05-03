# 型・バグ レビューレポート

---

## 1. 🔴 構造体レイアウトバグ — `statfs.f_fsid1 / f_fsid2`

**ファイル**: `LinuxDotNet.SystemInfo/NativeMethods.cs`  
**影響**: `FileSystemUsage` の全フィールド値が壊れる（深刻）

### 問題

```csharp
// 現状 — 8 bytes × 2 = 16 bytes
public long f_fsid1;
public long f_fsid2;
```

Linux カーネルの `__kernel_fsid_t` は `int val[2]` = **8 bytes** (4 bytes × 2) だが、  
`long` (8 bytes × 2) = **16 bytes** として宣言されているため、  
それ以降のフィールド (`f_namelen`, `f_frsize`, `f_flags`, `f_spare*`) が **+8 bytes ずれる**。

| フィールド | 正しいオフセット | 現状のオフセット |
|---|---|---|
| `f_ffree` | 48 | 48 |
| `f_fsid` (2 × int) | 56..63 | — |
| `f_namelen` | 64 | 72 ❌ |
| `f_frsize` | 72 | 80 ❌ |
| `f_flags` | 80 | 88 ❌ |

### 修正

```csharp
// 修正後 — 4 bytes × 2 = 8 bytes (正しいレイアウト)
public int f_fsid1;
public int f_fsid2;
```

---

## 2. 🔴 バグ — `SystemStat.Forks` が `ExtractInt32` で読み取られている

**ファイル**: `LinuxDotNet.SystemInfo/SystemStat.cs`

### 問題

プロパティは `long` なのに、読み取りメソッドが `ExtractInt32`（戻り値 `int`）になっている。  
長時間稼働サーバーでは `/proc/stat` の `processes` が `int.MaxValue` (~21 億) を超えうる。

```csharp
public long Forks { get; private set; }

// ↓ Update() 内
else if (span.StartsWith("processes"))
{
    Forks = ExtractInt32(span);   // ❌ int で切り詰め
}
```

### 修正

```csharp
Forks = ExtractInt64(span);       // ✅ long で正確に読む
```

---

## 3. 🟠 型の符号・桁 — 各プロジェクトのメトリクス値

Linux カーネルの proc/sysfs から読み取るカウンタはすべて **unsigned** の累積値。  
`long` (signed) ではなく `ulong` が適切。

### 3-1. `CpuStat` (SystemInfo/SystemStat.cs)

`/proc/stat` の CPU 時間はジフィーの unsigned カウンタ。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `User`, `Nice`, `System`, `Idle`, `IoWait`, `Irq`, `SoftIrq`, `Steal`, `Guest`, `GuestNice` | `long` | `ulong` |
| `Interrupt`, `ContextSwitch`, `Forks`, `SoftIrq` (SystemStat) | `long` | `ulong` |

### 3-2. `DiskStatEntry` (SystemInfo/DiskStat.cs)

`/proc/diskstats` のフィールドはすべて unsigned 64-bit。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `ReadCompleted`, `ReadMerged`, `ReadSectors`, `ReadTime` | `long` | `ulong` |
| `WriteCompleted`, `WriteMerged`, `WriteSectors`, `WriteTime` | `long` | `ulong` |
| `IosInProgress`, `IoTime`, `WeightIoTime` | `long` | `ulong` |

### 3-3. `NetworkStatEntry` (SystemInfo/NetworkStat.cs)

`/proc/net/dev` のカウンタはすべて unsigned。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `RxBytes`, `RxPackets`, `RxErrors`, `RxDropped`, `RxFifo`, `RxFrame`, `RxCompressed`, `RxMulticast` | `long` | `ulong` |
| `TxBytes`, `TxPackets`, `TxErrors`, `TxDropped`, `TxFifo`, `TxCollisions`, `TxCarrier`, `TxCompressed` | `long` | `ulong` |

### 3-4. `WirelessStatEntry` (SystemInfo/WirelessStat.cs)

廃棄パケット数などは unsigned カウンタ。  
※ `SignalLevel` / `NoiseLevel` は dBm (負値あり) なので `double` のまま正しい。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `DiscardedNetworkId`, `DiscardedCrypt`, `DiscardedFragment`, `DiscardedRetry`, `DiscardedMisc`, `MissedBeacon` | `long` | `ulong` |

### 3-5. `VirtualMemoryStat` (SystemInfo/VirtualMemoryStat.cs)

`/proc/vmstat` の全値は unsigned ページカウンタ。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `PageIn`, `PageOut`, `SwapIn`, `SwapOut`, `PageFaults`, `MajorPageFaults` | `long` | `ulong` |
| `StealKernel`, `StealDirect`, `ScanKernel`, `ScanDirect`, `OutOfMemoryKiller` | `long` | `ulong` |

### 3-6. `MemoryStat` (SystemInfo/MemoryStat.cs)

`/proc/meminfo` の値は kB 単位の unsigned 値。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `MemoryTotal`, `MemoryAvailable`, `MemoryFree`, `Buffers`, `Cached` ほか全フィールド | `long` | `ulong` |

### 3-7. `FileHandleStat` (SystemInfo/FileHandleStat.cs)

`/proc/sys/fs/file-nr` の値は unsigned カウンタ。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `Allocated`, `Used`, `Max` | `long` | `ulong` |

### 3-8. `BatteryDevice` (SystemInfo/BatteryDevice.cs)

| プロパティ | 単位 | 現状 | 推奨 | 備考 |
|---|---|---|---|---|
| `Voltage` | µV | `long` | `ulong` | 常に正 |
| `Current` | µA | `long` | **`long` のまま** | 充放電方向で負値あり |
| `Charge`, `ChargeFull` | µAh | `long` | `ulong` | 常に正 |

### 3-9. `CpuDevice` (SystemInfo/CpuDevice.cs)

| プロパティ | 単位 | 現状 | 推奨 |
|---|---|---|---|
| `CpuCore.Frequency` | kHz | `long` | `ulong` |
| `CpuPower.Energy` | µJ | `long` | `ulong` |

### 3-10. `HardwareInfo` (SystemInfo/HardwareInfo.cs)

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `CpuFrequencyMax` | `long` | `ulong` |
| `L1DCacheSize`, `L1ICacheSize`, `L2CacheSize`, `L3CacheSize` | `long` | `ulong` |
| `MemoryTotal`, `PageSize` | `long` | `ulong` |

### 3-11. `ProcessInfo` (SystemInfo/ProcessInfo.cs)

`/proc/[pid]/stat` の fault カウンタは unsigned。

| プロパティ | 現状 | 推奨 |
|---|---|---|
| `MajorFaults`, `MinorFaults` | `long` | `ulong` |

---

## 4. 🟠 型の符号 — `SmartAttribute.Flags`

**ファイル**: `LinuxDotNet.Disk/SmartAttribute.cs`

SMART の flags フィールドは 16-bit の unsigned ビットフラグ。

```csharp
// 現状
public short Flags { get; set; }   // ❌ signed

// 推奨
public ushort Flags { get; set; }  // ✅ unsigned
```

---

## 5. 🟠 `nint` / `IntPtr` の混在

**ファイル**: `LinuxDotNet.Video4Linux2/NativeMethods.cs`, `VideoCapture.cs`

### 問題

同一プロジェクト内で `nint`/`nuint` と `IntPtr`/`UIntPtr` が混在している。

| 箇所 | 現状 | 分類 |
|---|---|---|
| `v4l2_buffer.tv_sec` | `nint` | 構造体フィールド |
| `v4l2_buffer.tv_usec` | `nint` | 構造体フィールド |
| `v4l2_buffer_m.userptr` | `nuint` | 構造体フィールド |
| `mmap(IntPtr addr, ...)` | `IntPtr` | P/Invoke 宣言 |
| `VideoCapture.buffers[]` | `IntPtr[]` | マネージドコード |
| `FrameBuffer.buffer` | `IntPtr` | マネージドコード |

### 追加問題: `tv_sec` / `tv_usec` の型が意味的に不適切

`nint` は「ポインタ相当のネイティブサイズ整数」を意味するが、  
`tv_sec` / `tv_usec` は **時刻値** であり `long` が適切。  
64-bit 環境では `nint == long` なので動作はするが、意図が不明確になる。

```csharp
// 現状
public nint tv_sec;
public nint tv_usec;

// 推奨
public long tv_sec;    // Linux: __kernel_time_t = long (64-bit)
public long tv_usec;   // Linux: __kernel_suseconds_t = long (64-bit)
```

### 統一方針

- ポインタ相当の値: `IntPtr` / `UIntPtr` (既存コードに合わせる) または `nint` / `nuint` に統一
- 時刻・サイズ等のスカラー値: `long` / `ulong` など意味に合った型を使用

---

## 6. 🟡 `mmap` の `length` / `offset` 引数型

**ファイル**: `LinuxDotNet.Video4Linux2/NativeMethods.cs`

```csharp
// 現状
public static extern IntPtr mmap(IntPtr addr, int length, int prot, int flags, int fd, int offset);
```

Linux の実際のシグネチャ:
```c
void *mmap(void *addr, size_t length, int prot, int flags, int fd, off_t offset);
```

| パラメータ | Linux 型 | 現状 C# | 問題 |
|---|---|---|---|
| `length` | `size_t` (unsigned, 64-bit) | `int` | 2 GB 超のマッピング不可 |
| `offset` | `off_t` (signed, 64-bit) | `int` | 2 GB 超のオフセット不可 |

V4L2 用途では現状問題になりにくいが、将来のバッファサイズ増大に備えて修正推奨。

```csharp
// 推奨
public static extern IntPtr mmap(IntPtr addr, nuint length, int prot, int flags, int fd, long offset);
```

---

## 優先度サマリー

| 優先度 | 項目 | ファイル |
|---|---|---|
| 🔴 Critical | `statfs.f_fsid1/f_fsid2` が `long` → 構造体レイアウト破損 | `SystemInfo/NativeMethods.cs` |
| 🔴 Critical | `Forks` を `ExtractInt32` で読み取り → long の意味なし | `SystemInfo/SystemStat.cs` |
| 🟠 High | 各カウンタ `long` → `ulong` (DiskStat, NetworkStat, WirelessStat, CpuStat, VmStat, MemStat, FileHandleStat, Battery, Cpu, Hardware, ProcessInfo) | 各 cs |
| 🟠 High | `SmartAttribute.Flags` が `short` → `ushort` | `Disk/SmartAttribute.cs` |
| 🟠 High | `v4l2_buffer.tv_sec/tv_usec` が `nint` → `long` | `Video4Linux2/NativeMethods.cs` |
| 🟡 Medium | `nint`/`IntPtr` の混在を統一 | `Video4Linux2/NativeMethods.cs`, `VideoCapture.cs` |
| 🟡 Medium | `mmap` の `length`/`offset` が `int` → `nuint`/`long` | `Video4Linux2/NativeMethods.cs` |
