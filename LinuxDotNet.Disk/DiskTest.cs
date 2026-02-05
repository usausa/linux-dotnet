// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
#pragma warning disable CA1031
#pragma warning disable CA1310
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
namespace LinuxDotNet.Disk;

using System.Text.RegularExpressions;

// ディスクタイプのenum
public enum DiskType
{
    Unknown,
    NVMe,
    SATA,
    SCSI,
    MMC,      // SDカード、eMMC
    VirtIO,   // 仮想ディスク
    IDE
}

// ディスク情報を格納するクラス
public class DiskInformation
{
    public string DeviceName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DiskType Type { get; set; } = DiskType.Unknown;
    public string Serial { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Firmware { get; set; } = string.Empty;
    public int MajorNumber { get; set; }
    public int MinorNumber { get; set; }

    public override string ToString()
    {
        return $"Device:    {DeviceName}\n" +
               $"  Type:      {Type}\n" +
               $"  Model:     {(string.IsNullOrEmpty(Model) ? "N/A" : Model)}\n" +
               $"  Size:      {(string.IsNullOrEmpty(Size) ? "N/A" : Size)}\n" +
               $"  Serial:    {(string.IsNullOrEmpty(Serial) ? "N/A" : Serial)}\n" +
               $"  Firmware:  {(string.IsNullOrEmpty(Firmware) ? "N/A" : Firmware)}\n" +
               $"  Major:Minor: {MajorNumber}:{MinorNumber}";
    }
}

public sealed class DiskScanner
{
    private const string SysBlockPath = "/sys/block";

    // Linuxのメジャー番号定義
    // https://www.kernel.org/doc/Documentation/admin-guide/devices.txt
    private const int MajorNVMe = 259;        // NVMe
    private const int MajorSCSI = 8;          // SCSI disk (sd*)
    private const int MajorIDE1 = 3;          // IDE disk (hda-hdb)
    private const int MajorIDE2 = 22;         // IDE disk (hdc-hdd)
    private const int MajorMMC = 179;         // MMC block device (SDカード、eMMC)
    private const int MajorVirtIO = 252;      // VirtIO block device

    // sysfsから文字列を読み込む
    private static string? ReadSysfsString(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }
            var content = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // デバイスのメジャー番号とマイナー番号を取得
    private static (int Major, int Minor) GetDeviceNumbers(string deviceName)
    {
        var devPath = Path.Combine(SysBlockPath, deviceName, "dev");
        var devStr = ReadSysfsString(devPath);

        if (devStr == null)
        {
            return (-1, -1);
        }

        var parts = devStr.Split(':');
        if (parts.Length != 2)
        {
            return (-1, -1);
        }

        var major = int.TryParse(parts[0], out var maj) ? maj : -1;
        var minor = int.TryParse(parts[1], out var min) ? min : -1;

        return (major, minor);
    }

    // メジャー番号からディスクタイプを判定
    private static DiskType GetDiskTypeFromMajor(int majorNumber)
    {
        return majorNumber switch
        {
            MajorNVMe => DiskType.NVMe,
            MajorSCSI => DiskType.SATA, // SATAもSCSIサブシステムを使用
            MajorIDE1 or MajorIDE2 => DiskType.IDE,
            MajorMMC => DiskType.MMC,
            MajorVirtIO => DiskType.VirtIO,
            _ => DiskType.Unknown
        };
    }

    // NVMeデバイスかどうかを厳密に判定（メジャー番号ベース）
    private static bool IsNvmeDevice(int majorNumber)
    {
        return majorNumber == MajorNVMe;
    }

    // MMCデバイスかどうかを判定
    private static bool IsMmcDevice(int majorNumber)
    {
        return majorNumber == MajorMMC;
    }

    // 物理ディスクかどうかを判定（パーティションを除外）
    private static bool IsPhysicalDisk(string deviceName)
    {
        // NVMeデバイスの場合
        if (deviceName.StartsWith("nvme"))
        {
            // nvme0n1p1のような形式はパーティション
            var match = Regex.Match(deviceName, @"nvme\d+n\d+p\d+");
            return !match.Success;
        }

        // SATA/SCSIデバイスの場合（sd*, hd*）
        if (deviceName.StartsWith("sd") || deviceName.StartsWith("hd"))
        {
            // sda1のような形式はパーティション
            return !Regex.IsMatch(deviceName, @"^[sh]d[a-z]+\d+$");
        }

        // 仮想ディスク（vd*）
        if (deviceName.StartsWith("vd"))
        {
            return !Regex.IsMatch(deviceName, @"^vd[a-z]+\d+$");
        }

        // MMCデバイス（mmcblk*）
        if (deviceName.StartsWith("mmcblk"))
        {
            // mmcblk0p1のような形式はパーティション
            return !Regex.IsMatch(deviceName, @"^mmcblk\d+p\d+$");
        }

        return false;
    }

    // ディスクサイズを取得
    private static string GetDiskSize(string deviceName)
    {
        var sizePath = Path.Combine(SysBlockPath, deviceName, "size");
        var sizeStr = ReadSysfsString(sizePath);

        if (sizeStr == null || !ulong.TryParse(sizeStr, out var sectors))
        {
            return string.Empty;
        }

        var bytes = sectors * 512; // 通常は512バイト/セクタ

        // ReSharper disable InconsistentNaming
        const ulong TB = 1UL << 40;
        const ulong GB = 1UL << 30;
        const ulong MB = 1UL << 20;
        // ReSharper restore InconsistentNaming

        if (bytes >= TB)
        {
            return $"{(double)bytes / TB:F2} TB";
        }
        if (bytes >= GB)
        {
            return $"{(double)bytes / GB:F2} GB";
        }
        if (bytes >= MB)
        {
            return $"{(double)bytes / MB:F2} MB";
        }
        return $"{bytes} bytes";
    }

    // NVMeディスクの情報を取得
    private static void GetNvmeInfo(string deviceName, DiskInformation info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");

        // モデル名
        var model = ReadSysfsString(Path.Combine(devicePath, "model"));
        if (model != null)
        {
            info.Model = model;
        }

        // シリアル番号
        var serial = ReadSysfsString(Path.Combine(devicePath, "serial"));
        if (serial != null)
        {
            info.Serial = serial;
        }

        // ファームウェアバージョン
        var firmware = ReadSysfsString(Path.Combine(devicePath, "firmware_rev"));
        if (firmware != null)
        {
            info.Firmware = firmware;
        }
    }

    // SATA/SCSI/IDEディスクの情報を取得
    private static void GetScsiInfo(string deviceName, DiskInformation info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");

        // ベンダー情報
        var vendor = ReadSysfsString(Path.Combine(devicePath, "vendor"));

        // モデル名
        var model = ReadSysfsString(Path.Combine(devicePath, "model"));

        // ベンダー名とモデル名を結合
        if (!string.IsNullOrEmpty(vendor) && !string.IsNullOrEmpty(model))
        {
            info.Model = $"{vendor} {model}";
        }
        else if (!string.IsNullOrEmpty(model))
        {
            info.Model = model;
        }
        else if (!string.IsNullOrEmpty(vendor))
        {
            info.Model = vendor;
        }

        // リビジョン（ファームウェア）
        var revision = ReadSysfsString(Path.Combine(devicePath, "rev"));
        if (revision != null)
        {
            info.Firmware = revision;
        }
    }

    // MMC（SDカード、eMMC）の情報を取得
    private static void GetMmcInfo(string deviceName, DiskInformation info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");

        // デバイス名（モデル）
        var name = ReadSysfsString(Path.Combine(devicePath, "name"));
        if (name != null)
        {
            info.Model = name;
        }

        // CID（カードID）
        var cid = ReadSysfsString(Path.Combine(devicePath, "cid"));
        if (cid != null)
        {
            info.Serial = cid;
        }

        // タイプ（SD/MMC/eMMC）
        var type = ReadSysfsString(Path.Combine(devicePath, "type"));
        if (type != null)
        {
            info.Model = $"{type} {info.Model}".Trim();
        }

        // ファームウェアバージョン
        var fwrev = ReadSysfsString(Path.Combine(devicePath, "fwrev"));
        if (fwrev != null)
        {
            info.Firmware = fwrev;
        }
    }

    // ディスク情報を取得
    private static DiskInformation? GetDiskInfo(string deviceName)
    {
        try
        {
            var (major, minor) = GetDeviceNumbers(deviceName);
            if (major == -1)
            {
                return null;
            }

            var diskType = GetDiskTypeFromMajor(major);

            var info = new DiskInformation
            {
                DeviceName = $"/dev/{deviceName}",
                Size = GetDiskSize(deviceName),
                Type = diskType,
                MajorNumber = major,
                MinorNumber = minor
            };

            // デバイスタイプに応じて情報取得
            if (IsNvmeDevice(major))
            {
                GetNvmeInfo(deviceName, info);
            }
            else if (IsMmcDevice(major))
            {
                GetMmcInfo(deviceName, info);
            }
            else
            {
                GetScsiInfo(deviceName, info);
            }

            return info;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // すべてのディスクをスキャン
    public static IReadOnlyList<DiskInformation> ScanDisks()
    {
        var disks = new List<DiskInformation>();

        if (!Directory.Exists(SysBlockPath))
        {
            Console.Error.WriteLine($"Error: {SysBlockPath} does not exist.");
            return disks;
        }

        try
        {
            var directories = Directory.GetDirectories(SysBlockPath)
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .Cast<string>();

            foreach (var deviceName in directories)
            {
                // ループデバイスやramディスク、デバイスマッパーをスキップ
                if (deviceName.StartsWith("loop") ||
                    deviceName.StartsWith("ram") ||
                    deviceName.StartsWith("dm-"))
                {
                    continue;
                }

                // 物理ディスクのみを対象
                if (!IsPhysicalDisk(deviceName))
                {
                    continue;
                }

                var info = GetDiskInfo(deviceName);
                if (info != null)
                {
                    disks.Add(info);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error scanning disks: {ex.Message}");
        }

        return disks;
    }

    // ディスク情報を表示
    public static void DisplayDisks(IReadOnlyList<DiskInformation> disks)
    {
        Console.WriteLine("=== Physical Disk Information ===\n");

        if (disks.Count == 0)
        {
            Console.WriteLine("No physical disks found.");
            return;
        }

        for (var i = 0; i < disks.Count; i++)
        {
            Console.WriteLine($"Disk #{i + 1}:");
            Console.WriteLine($"  {disks[i]}");
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {disks.Count} disk(s) found.");
    }
}

public static class DiskTest
{
    public static void Main()
    {
        // root権限チェック
        if (Environment.UserName != "root")
        {
            Console.WriteLine("Warning: Running without root privileges. Some information may not be available.\n");
        }

        // ディスクをスキャン
        var disks = DiskScanner.ScanDisks();

        // 結果を表示
        DiskScanner.DisplayDisks(disks);
    }
}
