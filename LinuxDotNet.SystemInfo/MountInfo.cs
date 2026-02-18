namespace LinuxDotNet.SystemInfo;

using static LinuxDotNet.SystemInfo.NativeMethods;

public sealed class MountInfo
{
    // ReSharper disable StringLiteralTypo
    private static readonly HashSet<string> LocalFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "ext2", "ext3", "ext4", "xfs", "btrfs", "zfs", "ntfs", "vfat", "fat32", "exfat",
        "f2fs", "jfs", "reiserfs", "ufs", "hfs", "hfsplus", "apfs",
    };
    // ReSharper restore StringLiteralTypo

    // ReSharper disable StringLiteralTypo
    private static readonly HashSet<string> VirtualFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "sysfs", "proc", "devtmpfs", "devpts", "tmpfs", "securityfs", "cgroup", "cgroup2",
        "pstore", "bpf", "debugfs", "tracefs", "hugetlbfs", "mqueue", "configfs", "fusectl",
        "ramfs", "rpc_pipefs", "overlay", "aufs", "squashfs",
    };
    // ReSharper restore StringLiteralTypo

    public string MountPoint { get; }

    public string TypeName { get; }

    public string DeviceName { get; }

    // TODO
    public string Options { get; }

    public bool IsReadOnly => Options.Contains("ro", StringComparison.Ordinal);

    public bool IsLocal { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private MountInfo(string mountPoint, string typeName, string deviceName, string options, bool isLocal)
    {
        MountPoint = mountPoint;
        TypeName = typeName;
        DeviceName = deviceName;
        Options = options;
        IsLocal = isLocal;
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<MountInfo> GetMounts(bool includeVirtual = false)
    {
        return GetMountsCore(mount =>
        {
            if (!includeVirtual && VirtualFileSystems.Contains(mount.TypeName))
            {
                return false;
            }

            if (!includeVirtual && mount.DeviceName == "none")
            {
                return false;
            }

            return true;
        });
    }

    internal static IReadOnlyList<MountInfo> GetMountsForDevice(string deviceName)
    {
        var devicePath = $"/dev/{deviceName}";
        return GetMountsCore(mount => mount.DeviceName == devicePath);
    }

    private static List<MountInfo> GetMountsCore(Func<MountInfo, bool> filter)
    {
        var result = new List<MountInfo>();

        using var reader = new StreamReader("/proc/mounts");
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                continue;
            }

            var device = parts[0];
            var mountPoint = parts[1].Replace("\\040", " ").Replace("\\011", "\t");
            var fsType = parts[2];
            var options = parts[3];

            var isLocal = LocalFileSystems.Contains(fsType);

            var mount = new MountInfo(mountPoint, fsType, device, options, isLocal);

            if (filter(mount))
            {
                result.Add(mount);
            }
        }

        return result;
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
