namespace LinuxDotNet.SystemInfo;

using System.Text;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class DiskStatEntry
{
    internal bool Live { get; set; }

    internal byte[] RawName { get; }

    public string Name { get; }

    public ulong ReadCompleted { get; internal set; }

    public ulong ReadMerged { get; internal set; }

    public ulong ReadSectors { get; internal set; }

    public ulong ReadTime { get; internal set; }

    public ulong WriteCompleted { get; internal set; }

    public ulong WriteMerged { get; internal set; }

    public ulong WriteSectors { get; internal set; }

    public ulong WriteTime { get; internal set; }

    public ulong IosInProgress { get; internal set; }

    public ulong IoTime { get; internal set; }

    public ulong WeightIoTime { get; internal set; }

    internal DiskStatEntry(ReadOnlySpan<byte> name)
    {
        RawName = name.ToArray();
        Name = Encoding.UTF8.GetString(name);
    }
}

public sealed class DiskStat : IDisposable
{
    // Counters after the device name
    private const int ValueCount = 11;

    private readonly KernelFile file;

    private readonly List<DiskStatEntry> devices = [];

    private bool disposed;

    public DateTime UpdateAt { get; internal set; }

    public IReadOnlyList<DiskStatEntry> Devices => devices;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private DiskStat(KernelFile file)
    {
        this.file = file;
    }

    internal static DiskStat Create()
    {
        var instance = new DiskStat(new KernelFile("/proc/diskstats"));
        instance.Update();
        return instance;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        file.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!file.Read())
        {
            return false;
        }

        foreach (var item in devices)
        {
            item.Live = false;
        }

        var values = (Span<ulong>)stackalloc ulong[ValueCount];
        var added = false;
        var remaining = file.Content;
        while (TryReadLine(ref remaining, out var line))
        {
            // major minor name and the counters (14 or more tokens)
            var deviceClass = (DeviceClass)ParseInt32(NextToken(ref line));
            if (!deviceClass.IsPhysicalStorage())
            {
                continue;
            }

            _ = NextToken(ref line);
            var name = NextToken(ref line);
            if (!TryParseValues(ref line, values))
            {
                continue;
            }

            var device = default(DiskStatEntry);
            foreach (var item in devices)
            {
                if (name.SequenceEqual(item.RawName))
                {
                    device = item;
                    break;
                }
            }

            if (device == null)
            {
                device = new DiskStatEntry(name);
                devices.Add(device);
                added = true;
            }

            device.Live = true;

            device.ReadCompleted = values[0];
            device.ReadMerged = values[1];
            device.ReadSectors = values[2];
            device.ReadTime = values[3];
            device.WriteCompleted = values[4];
            device.WriteMerged = values[5];
            device.WriteSectors = values[6];
            device.WriteTime = values[7];
            device.IosInProgress = values[8];
            device.IoTime = values[9];
            device.WeightIoTime = values[10];
        }

        for (var i = devices.Count - 1; i >= 0; i--)
        {
            if (!devices[i].Live)
            {
                devices.RemoveAt(i);
            }
        }

        if (added)
        {
            devices.Sort(static (x, y) => String.Compare(x.Name, y.Name, StringComparison.Ordinal));
        }

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static bool TryParseValues(ref ReadOnlySpan<byte> line, scoped Span<ulong> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            var token = NextToken(ref line);
            if (token.IsEmpty)
            {
                return false;
            }

            values[i] = ParseUInt64(token);
        }

        return true;
    }
}
