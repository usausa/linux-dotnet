namespace LinuxDotNet.SystemInfo;

public sealed class DiskStatEntry
{
    internal bool Live { get; set; }

    public string Name { get; }

    public long ReadCompleted { get; internal set; }

    public long ReadMerged { get; internal set; }

    public long ReadSectors { get; internal set; }

    public long ReadTime { get; internal set; }

    public long WriteCompleted { get; internal set; }

    public long WriteMerged { get; internal set; }

    public long WriteSectors { get; internal set; }

    public long WriteTime { get; internal set; }

    public long IosInProgress { get; internal set; }

    public long IoTime { get; internal set; }

    public long WeightIoTime { get; internal set; }

    internal DiskStatEntry(string name)
    {
        Name = name;
    }
}

public sealed class DiskStat
{
    private readonly List<DiskStatEntry> devices = new();

    public DateTime UpdateAt { get; internal set; }

    public IReadOnlyList<DiskStatEntry> Devices => devices;

    internal DiskStat()
    {
        Update();
    }

    public bool Update()
    {
        foreach (var item in devices)
        {
            item.Live = false;
        }

        var range = (Span<Range>)stackalloc Range[21];
        using var reader = new StreamReader("/proc/diskstats");
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 14)
            {
                continue;
            }

            var major = Int32.TryParse(span[range[0]], out var m) ? m : 0;
            if (!Helper.IsTargetDriveType(major))
            {
                continue;
            }

            var name = span[range[2]];
            var device = default(DiskStatEntry);
            foreach (var item in devices)
            {
                if (item.Name == name)
                {
                    device = item;
                    break;
                }
            }

            if (device == null)
            {
                device = new DiskStatEntry(name.ToString());
                devices.Add(device);
            }

            device.Live = true;

            device.ReadCompleted = Int64.TryParse(span[range[3]], out var readCompleted) ? readCompleted : 0;
            device.ReadMerged = Int64.TryParse(span[range[4]], out var readMerged) ? readMerged : 0;
            device.ReadSectors = Int64.TryParse(span[range[5]], out var readSectors) ? readSectors : 0;
            device.ReadTime = Int64.TryParse(span[range[6]], out var readTime) ? readTime : 0;
            device.WriteCompleted = Int64.TryParse(span[range[7]], out var writeCompleted) ? writeCompleted : 0;
            device.WriteMerged = Int64.TryParse(span[range[8]], out var writeMerged) ? writeMerged : 0;
            device.WriteSectors = Int64.TryParse(span[range[9]], out var writeSectors) ? writeSectors : 0;
            device.WriteTime = Int64.TryParse(span[range[10]], out var writeTime) ? writeTime : 0;
            device.IosInProgress = Int64.TryParse(span[range[11]], out var iosInProgress) ? iosInProgress : 0;
            device.IoTime = Int64.TryParse(span[range[12]], out var ioTime) ? ioTime : 0;
            device.WeightIoTime = Int64.TryParse(span[range[13]], out var weightIoTime) ? weightIoTime : 0;
        }

        for (var i = devices.Count - 1; i >= 0; i--)
        {
            if (!devices[i].Live)
            {
                devices.RemoveAt(i);
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
}
