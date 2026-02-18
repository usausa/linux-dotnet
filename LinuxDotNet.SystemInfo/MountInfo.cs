namespace LinuxDotNet.SystemInfo;

using static LinuxDotNet.SystemInfo.NativeMethods;

[Flags]
public enum MountOption
{
    None = 0,
    // Read/Write options
    ReadOnly = 1 << 0,
    ReadWrite = 1 << 1,
    // Security options
    NoSuid = 1 << 2,
    NoExec = 1 << 3,
    NoDev = 1 << 4,
    // Time options
    NoAccessTime = 1 << 5,
    RelativeAccessTime = 1 << 6,
    StrictAccessTime = 1 << 7,
    // Synchronization options
    Sync = 1 << 8,
    Async = 1 << 9,
    DirectorySync = 1 << 10,
    // Other common options
    NoUser = 1 << 11,
    User = 1 << 12,
    Auto = 1 << 13,
    NoAuto = 1 << 14,
    Defaults = 1 << 15
}

public static class MountOptionExtensions
{
    public static bool IsReadonly(this MountOption option) => (option & MountOption.ReadOnly) != 0;
}

public sealed class MountInfo
{
    // ReSharper disable StringLiteralTypo
    private static readonly HashSet<string> LocalFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "ext2", "ext3", "ext4", "xfs", "btrfs", "zfs", "ntfs", "vfat", "fat32", "exfat",
        "f2fs", "jfs", "reiserfs", "ufs", "hfs", "hfsplus", "apfs"
    };
    // ReSharper restore StringLiteralTypo

    // ReSharper disable StringLiteralTypo
    private static readonly HashSet<string> VirtualFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "sysfs", "proc", "devtmpfs", "devpts", "tmpfs", "securityfs", "cgroup", "cgroup2",
        "pstore", "bpf", "debugfs", "tracefs", "hugetlbfs", "mqueue", "configfs", "fusectl",
        "ramfs", "rpc_pipefs", "overlay", "aufs", "squashfs"
    };
    // ReSharper restore StringLiteralTypo

    // ReSharper disable StringLiteralTypo
    private static readonly Dictionary<string, MountOption> OptionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ro"] = MountOption.ReadOnly,
        ["rw"] = MountOption.ReadWrite,
        ["nosuid"] = MountOption.NoSuid,
        ["noexec"] = MountOption.NoExec,
        ["nodev"] = MountOption.NoDev,
        ["noatime"] = MountOption.NoAccessTime,
        ["relatime"] = MountOption.RelativeAccessTime,
        ["strictatime"] = MountOption.StrictAccessTime,
        ["sync"] = MountOption.Sync,
        ["async"] = MountOption.Async,
        ["dirsync"] = MountOption.DirectorySync,
        ["nouser"] = MountOption.NoUser,
        ["user"] = MountOption.User,
        ["auto"] = MountOption.Auto,
        ["noauto"] = MountOption.NoAuto,
        ["defaults"] = MountOption.Defaults
    };
    // ReSharper restore StringLiteralTypo

    public string MountPoint { get; }

    public string FileSystem { get; }

    public string DeviceName { get; }

    public MountOption Option { get; }

    public bool IsLocal { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private MountInfo(string mountPoint, string fileSystem, string deviceName, MountOption option)
    {
        MountPoint = mountPoint;
        FileSystem = fileSystem;
        DeviceName = deviceName;
        Option = option;
        IsLocal = LocalFileSystems.Contains(fileSystem);
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<MountInfo> GetMounts(bool includeVirtual = false)
    {
        return GetMountsCore(x => includeVirtual || (!VirtualFileSystems.Contains(x.FileSystem) && (x.DeviceName != "none")));
    }

    internal static IReadOnlyList<MountInfo> GetMountsForDevice(string deviceName)
    {
        var devicePath = $"/dev/{deviceName}";
        return GetMountsCore(x => x.DeviceName == devicePath);
    }

    private static List<MountInfo> GetMountsCore(Func<MountInfo, bool> filter)
    {
        var list = new List<MountInfo>();

        var range = (Span<Range>)stackalloc Range[6];
        using var reader = new StreamReader("/proc/mounts");
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            var count = span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries);
            if (count < 4)
            {
                continue;
            }

            var device = span[range[0]].ToString();
            var mountPoint = span[range[1]].ToString().Replace("\\040", " ", StringComparison.Ordinal).Replace("\\011", "\t", StringComparison.Ordinal);
            var fsType = span[range[2]].ToString();
            var option = MountOption.None;
            foreach (var key in span[range[3]].ToString().Split(','))
            {
                if (OptionMap.TryGetValue(key, out var flag))
                {
                    option |= flag;
                }
            }

            var mount = new MountInfo(mountPoint, fsType, device, option);

            if (filter(mount))
            {
                list.Add(mount);
            }
        }

        return list;
    }

    //--------------------------------------------------------------------------------
    // Usage
    //--------------------------------------------------------------------------------

    public FileSystemUsage? GetUsage()
    {
        var buf = default(statfs);
        if (statfs64(MountPoint, ref buf) != 0)
        {
            return null;
        }

        return new FileSystemUsage(
            buf.f_blocks * buf.f_bsize,
            buf.f_bfree * buf.f_bsize,
            buf.f_bavail * buf.f_bsize,
            buf.f_bsize,
            buf.f_files,
            buf.f_ffree);
    }
}
