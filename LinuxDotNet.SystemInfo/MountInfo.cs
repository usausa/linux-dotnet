namespace LinuxDotNet.SystemInfo;

using System.Text;

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
#pragma warning disable IDE0028
    // ReSharper disable StringLiteralTypo
    private static readonly HashSet<string> LocalFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "ext2", "ext3", "ext4", "xfs", "btrfs", "zfs", "ntfs", "vfat", "fat32", "exfat",
        "f2fs", "jfs", "reiserfs", "ufs", "hfs", "hfsplus", "apfs"
    };

    private static readonly HashSet<string> VirtualFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "sysfs", "proc", "devtmpfs", "devpts", "tmpfs", "securityfs", "cgroup", "cgroup2",
        "pstore", "bpf", "debugfs", "tracefs", "hugetlbfs", "mqueue", "configfs", "fusectl",
        "ramfs", "rpc_pipefs", "overlay", "aufs", "squashfs"
    };

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
#pragma warning restore IDE0028

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

#if NET9_0_OR_GREATER
        var optionLookup = OptionMap.GetAlternateLookup<ReadOnlySpan<char>>();
#endif
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

            var device = DecodeOctalEscape(span[range[0]]);
            var mountPoint = DecodeOctalEscape(span[range[1]]);
            var fsType = DecodeOctalEscape(span[range[2]]);
            var option = MountOption.None;
            var remaining = span[range[3]];
            while (!remaining.IsEmpty)
            {
                var commaIndex = remaining.IndexOf(',');
                var key = commaIndex >= 0 ? remaining[..commaIndex] : remaining;
#if NET9_0_OR_GREATER
                if (optionLookup.TryGetValue(key, out var value))
#else
                if (OptionMap.TryGetValue(key.ToString(), out var value))
#endif
                {
                    option |= value;
                }

                if (commaIndex < 0)
                {
                    break;
                }

                remaining = remaining[(commaIndex + 1)..];
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
    // Helper
    //--------------------------------------------------------------------------------

    private static bool IsOctalDigit(char value) => value is >= '0' and <= '7';

    private static string DecodeOctalEscape(ReadOnlySpan<char> value)
    {
        var index = value.IndexOf('\\');
        if (index < 0)
        {
            return value.ToString();
        }

        var builder = new StringBuilder(value.Length);
        while (true)
        {
            builder.Append(value[..index]);

            if ((index + 3 < value.Length) &&
                IsOctalDigit(value[index + 1]) &&
                IsOctalDigit(value[index + 2]) &&
                IsOctalDigit(value[index + 3]))
            {
                var code = ((value[index + 1] - '0') * 64) + ((value[index + 2] - '0') * 8) + (value[index + 3] - '0');
                builder.Append((char)code);
                value = value[(index + 4)..];
            }
            else
            {
                builder.Append('\\');
                value = value[(index + 1)..];
            }

            index = value.IndexOf('\\');
            if (index < 0)
            {
                builder.Append(value);
                return builder.ToString();
            }
        }
    }
}
