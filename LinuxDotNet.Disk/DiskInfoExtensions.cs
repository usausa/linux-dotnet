namespace LinuxDotNet.Disk;

using System;
using System.Text.RegularExpressions;

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
        var partitionPattern = GetPartitionPattern(disk.DiskType, disk.DeviceName);

        var index = 0u;
        foreach (var name in Directory.GetDirectories(blockPath).Select(Path.GetFileName).Where(x => x is not null && Regex.IsMatch(x, partitionPattern)).OrderBy(x => x))
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

    private static string GetPartitionPattern(DiskType diskType, string deviceName)
    {
        // NVMe: nvme0n1 -> nvme0n1p\d+
        if (diskType == DiskType.Nvme)
        {
            return $"^{Regex.Escape(deviceName)}p\\d+$";
        }

        // SATA/SCSI/VirtualIO: sda -> sda\d+, vda -> vda\d+
        return $"^{Regex.Escape(deviceName)}\\d+$";
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
