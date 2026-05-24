namespace LinuxDotNet.Disk;

public static class DiskInfoExtensions
{
    private const string SysBlockPath = "/sys/block";
    private const string ProcMountsPath = "/proc/mounts";

    public static IEnumerable<PartitionInfo> GetPartitions(this IDiskInfo disk)
    {
        var blockPath = Path.Combine(SysBlockPath, disk.DeviceName);
        if (!Directory.Exists(blockPath))
        {
            yield break;
        }

        var mountPoints = GetMountPoints();

        var index = 0u;
        foreach (var name in Directory.GetDirectories(blockPath).Select(Path.GetFileName).Where(x => x is not null && IsPartition(disk.DiskType, disk.DeviceName, x)).OrderBy(x => x))
        {
            var deviceName = $"/dev/{name}";
            var sectors = Helper.ReadFileAsUInt64(Path.Combine(blockPath, name!, "size")) ?? 0;
            mountPoints.TryGetValue(deviceName, out var mountInfo);

            yield return new PartitionInfo
            {
                Index = index++,
                DeviceName = deviceName,
                Name = name!,
                Size = sectors * disk.LogicalBlockSize,
                MountPoint = mountInfo?.MountPoint,
                FileSystem = mountInfo?.FileSystem
            };
        }
    }

    private static bool IsPartition(DiskType diskType, string deviceName, string name)
    {
        if (!name.StartsWith(deviceName, StringComparison.Ordinal) || (name.Length <= deviceName.Length))
        {
            return false;
        }

        if (diskType == DiskType.Nvme)
        {
            return (name[deviceName.Length] == 'p') && (name.Length > deviceName.Length + 1) && Char.IsAsciiDigit(name[^1]);
        }

        return Char.IsAsciiDigit(name[^1]);
    }

    private static Dictionary<string, (string MountPoint, string FileSystem)?> GetMountPoints()
    {
        var result = new Dictionary<string, (string MountPoint, string FileSystem)?>(StringComparer.Ordinal);

        var range = (Span<Range>)stackalloc Range[3];
        using var reader = new StreamReader(ProcMountsPath);
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 3)
            {
                continue;
            }

            var device = span[range[0]].ToString();
            if (device.StartsWith("/dev/", StringComparison.Ordinal))
            {
                var mountPoint = span[range[1]].ToString();
                var fileSystem = span[range[2]].ToString();
                result[device] = (mountPoint, fileSystem);
            }
        }

        return result;
    }
}
