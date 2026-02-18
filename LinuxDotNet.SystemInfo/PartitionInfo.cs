namespace LinuxDotNet.SystemInfo;

public sealed class PartitionInfo
{
    public string Name { get; init; } = default!;

    public DeviceClass DeviceClass { get; init; }

    public int No { get; init; }

    public ulong Blocks { get; init; }

    public MountInfo[] GetMounts() => MountInfo.GetMountsForDeviceName(Name);

    internal static IReadOnlyList<PartitionInfo> GetPartitions(bool includeAll = false)
    {
        var partitions = new List<PartitionInfo>();

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

            var deviceClass = Int32.TryParse(span[range[0]], out var major) ? (DeviceClass)major : DeviceClass.Unknown;
            if (!includeAll && !deviceClass.IsPhysicalStorage())
            {
                continue;
            }

            partitions.Add(new PartitionInfo
            {
                Name = span[range[3]].ToString(),
                DeviceClass = deviceClass,
                No = Int32.TryParse(span[range[1]], out var minor) ? minor : 0,
                Blocks = UInt64.TryParse(span[range[2]], out var blocks) ? blocks : 0
            });
        }

        return partitions;
    }
}
