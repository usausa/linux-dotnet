namespace LinuxDotNet.SystemInfo;

public sealed class PartitionInfo
{
    public string Name { get; }

    public IReadOnlyList<string> MountPoints { get; }

    internal PartitionInfo(string name, string[] mountPoints)
    {
        Name = name;
        MountPoints = mountPoints;
    }

    internal static IReadOnlyList<PartitionInfo> GetPartitions()
    {
        var partitions = new List<PartitionInfo>();
        var mounts = GetMounts();

        var range = (Span<Range>)stackalloc Range[5];
        using var reader = new StreamReader("/proc/partitions");
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 4)
            {
                continue;
            }

            var major = Int32.TryParse(span[range[0]], out var m) ? m : 0;
            if (!Helper.IsTargetDriveType(major))
            {
                continue;
            }

            var device = span[range[3]].ToString();
            if (!mounts.TryGetValue($"/dev/{device}", out var mountPoints))
            {
                continue;
            }

            partitions.Add(new PartitionInfo(device, mountPoints.ToArray()));
        }

        return partitions;
    }

    // TODO Mountsを返す？
    private static Dictionary<string, List<string>> GetMounts()
    {
        var mounts = new Dictionary<string, List<string>>();

        var range = (Span<Range>)stackalloc Range[3];
        using var reader = new StreamReader("/proc/mounts");
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 2)
            {
                continue;
            }

            var device = span[range[0]].ToString();
            var mountPoint = span[range[1]].ToString();

            if (!mounts.TryGetValue(device, out var list))
            {
                list = new List<string>();
                mounts[device] = list;
            }
            list.Add(mountPoint);
        }

        return mounts;
    }
}
