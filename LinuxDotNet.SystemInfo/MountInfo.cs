namespace LinuxDotNet.SystemInfo;

public sealed class MountInfo
{
    private static readonly HashSet<string> LocalFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "ext2", "ext3", "ext4", "xfs", "btrfs", "zfs", "ntfs", "vfat", "fat32", "exfat",
        "f2fs", "jfs", "reiserfs", "ufs", "hfs", "hfsplus", "apfs",
    };

    private static readonly HashSet<string> VirtualFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "sysfs", "proc", "devtmpfs", "devpts", "tmpfs", "securityfs", "cgroup", "cgroup2",
        "pstore", "bpf", "debugfs", "tracefs", "hugetlbfs", "mqueue", "configfs", "fusectl",
        "ramfs", "rpc_pipefs", "overlay", "aufs", "squashfs",
    };

    public string MountPoint { get; }

    public string TypeName { get; }

    public string DeviceName { get; }

    public string Options { get; }

    public bool IsReadOnly => Options.Contains("ro", StringComparison.Ordinal);

    public bool IsLocal { get; }

    private MountInfo(string mountPoint, string typeName, string deviceName, string options, bool isLocal)
    {
        MountPoint = mountPoint;
        TypeName = typeName;
        DeviceName = deviceName;
        Options = options;
        IsLocal = isLocal;
    }

    public FileSystemStat? GetFileSystemStat()
    {
        try
        {
            var buf = new NativeMethods.statfs();
            if (NativeMethods.statfs64(MountPoint, ref buf) != 0)
            {
                return null;
            }

            return new FileSystemStat(
                buf.f_blocks * buf.f_bsize,
                buf.f_bfree * buf.f_bsize,
                buf.f_bavail * buf.f_bsize,
                buf.f_bsize,
                buf.f_files,
                buf.f_ffree);
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<MountInfo> GetMounts(bool includeVirtual = false)
    {
        var result = new List<MountInfo>();

        if (!File.Exists("/proc/mounts"))
        {
            return [];
        }

        try
        {
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

                if (!includeVirtual && VirtualFileSystems.Contains(fsType))
                {
                    continue;
                }

                if (!includeVirtual && device == "none")
                {
                    continue;
                }

                var isLocal = LocalFileSystems.Contains(fsType);

                result.Add(new MountInfo(mountPoint, fsType, device, options, isLocal));
            }
        }
        catch
        {
            // Ignore
        }

        return [.. result];
    }

    internal static MountInfo[] GetMountsForDevice(string devicePath)
    {
        var result = new List<MountInfo>();

        if (!File.Exists("/proc/mounts"))
        {
            return [];
        }

        try
        {
            using var reader = new StreamReader("/proc/mounts");
            while (reader.ReadLine() is { } line)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                {
                    continue;
                }

                var device = parts[0];
                if (device != devicePath)
                {
                    continue;
                }

                var mountPoint = parts[1].Replace("\\040", " ").Replace("\\011", "\t");
                var fsType = parts[2];
                var options = parts[3];

                var isLocal = LocalFileSystems.Contains(fsType);

                result.Add(new MountInfo(mountPoint, fsType, device, options, isLocal));
            }
        }
        catch
        {
            // Ignore
        }

        return [.. result];
    }

    internal static MountInfo[] GetMountsForDeviceName(string deviceName)
    {
        return GetMountsForDevice($"/dev/{deviceName}");
    }
}
