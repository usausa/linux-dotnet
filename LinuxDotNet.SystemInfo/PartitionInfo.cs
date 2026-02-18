namespace LinuxDotNet.SystemInfo;

public sealed class PartitionInfo
{
    public string Name { get; }

    private readonly string path;

    private PartitionInfo(string name, string path)
    {
        Name = name;
        this.path = path;
    }

    public MountInfo[] GetMounts() => MountInfo.GetMountsForDevice(path);

    internal static IReadOnlyList<PartitionInfo> GetPartitions()
    {
        var partitions = new List<PartitionInfo>();
        var devicePaths = GetDevicePaths();

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
            var devicePath = $"/dev/{device}";
            if (!devicePaths.Contains(devicePath))
            {
                continue;
            }

            partitions.Add(new PartitionInfo(device, devicePath));
        }

        return partitions;
    }

    private static HashSet<string> GetDevicePaths()
    {
        var devicePaths = new HashSet<string>();

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
            devicePaths.Add(device);
        }

        return devicePaths;
    }
}
