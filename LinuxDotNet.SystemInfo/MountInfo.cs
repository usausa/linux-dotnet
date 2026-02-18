namespace LinuxDotNet.SystemInfo;

using System.Runtime.InteropServices;

public sealed record MountEntry
{
    public required string MountPoint { get; init; }

    public required string TypeName { get; init; }

    public required string DeviceName { get; init; }

    public required string Options { get; init; }

    public required ulong TotalSize { get; init; }

    public required ulong FreeSize { get; init; }

    public required ulong AvailableSize { get; init; }

    public required ulong BlockSize { get; init; }

    public double UsagePercent => TotalSize > 0 ? 100.0 * (TotalSize - AvailableSize) / TotalSize : 0;

    public required ulong TotalFiles { get; init; }

    public required ulong FreeFiles { get; init; }

    public bool IsReadOnly => Options.Contains("ro", StringComparison.Ordinal);

    public bool IsLocal { get; init; }
}

public static class MountInfo
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

    public static MountEntry[] GetMounts(bool includeVirtual = false)
    {
        var result = new List<MountEntry>();

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

                var stats = GetStatFs(mountPoint);
                if (stats is null)
                {
                    continue;
                }

                var isLocal = LocalFileSystems.Contains(fsType);

                result.Add(new MountEntry
                {
                    MountPoint = mountPoint,
                    TypeName = fsType,
                    DeviceName = device,
                    Options = options,
                    TotalSize = stats.TotalSize,
                    FreeSize = stats.FreeSize,
                    AvailableSize = stats.AvailableSize,
                    BlockSize = stats.BlockSize,
                    TotalFiles = stats.TotalFiles,
                    FreeFiles = stats.FreeFiles,
                    IsLocal = isLocal,
                });
            }
        }
        catch
        {
            // Ignore
        }

        return [.. result];
    }

    private sealed record StatFsResult
    {
        public required ulong TotalSize { get; init; }
        public required ulong FreeSize { get; init; }
        public required ulong AvailableSize { get; init; }
        public required ulong BlockSize { get; init; }
        public required ulong TotalFiles { get; init; }
        public required ulong FreeFiles { get; init; }
    }

    private static StatFsResult? GetStatFs(string path)
    {
        try
        {
            var buf = new statfs64();
            if (statfs64_native(path, ref buf) != 0)
            {
                return null;
            }

            return new StatFsResult
            {
                TotalSize = buf.f_blocks * buf.f_bsize,
                FreeSize = buf.f_bfree * buf.f_bsize,
                AvailableSize = buf.f_bavail * buf.f_bsize,
                BlockSize = buf.f_bsize,
                TotalFiles = buf.f_files,
                FreeFiles = buf.f_ffree,
            };
        }
        catch
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct statfs64
    {
        public ulong f_type;
        public ulong f_bsize;
        public ulong f_blocks;
        public ulong f_bfree;
        public ulong f_bavail;
        public ulong f_files;
        public ulong f_ffree;
        public long f_fsid1;
        public long f_fsid2;
        public ulong f_namelen;
        public ulong f_frsize;
        public ulong f_flags;
        public ulong f_spare1;
        public ulong f_spare2;
        public ulong f_spare3;
        public ulong f_spare4;
    }

    [DllImport("libc", EntryPoint = "statfs64", SetLastError = true)]
    private static extern int statfs64_native([MarshalAs(UnmanagedType.LPStr)] string path, ref statfs64 buf);
}

