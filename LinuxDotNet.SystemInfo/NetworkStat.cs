namespace LinuxDotNet.SystemInfo;

public sealed class NetworkStatEntry
{
    internal bool Live { get; set; }

    public string Interface { get; }

    public long RxBytes { get; internal set; }

    public long RxPackets { get; internal set; }

    public long RxErrors { get; internal set; }

    public long RxDropped { get; internal set; }

    public long RxFifo { get; internal set; }

    public long RxFrame { get; internal set; }

    public long RxCompressed { get; internal set; }

    public long RxMulticast { get; internal set; }

    public long TxBytes { get; internal set; }

    public long TxPackets { get; internal set; }

    public long TxErrors { get; internal set; }

    public long TxDropped { get; internal set; }

    public long TxFifo { get; internal set; }

    public long TxCollisions { get; internal set; }

    public long TxCarrier { get; internal set; }

    public long TxCompressed { get; internal set; }

    internal NetworkStatEntry(string interfaceName)
    {
        Interface = interfaceName;
    }
}

public sealed class NetworkStat
{
    private readonly List<NetworkStatEntry> interfaces = new();

    public DateTime UpdateAt { get; internal set; }

    public IReadOnlyList<NetworkStatEntry> Interfaces => interfaces;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal NetworkStat()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        foreach (var network in interfaces)
        {
            network.Live = false;
        }

        var range = (Span<Range>)stackalloc Range[18];
        using var reader = new StreamReader("/proc/net/dev");
        reader.ReadLine();
        var added = false;
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 17)
            {
                continue;
            }

            var name = span[range[0]].TrimEnd(':');
            var network = default(NetworkStatEntry);
            foreach (var item in interfaces)
            {
                if (item.Interface == name)
                {
                    network = item;
                    break;
                }
            }

            if (network == null)
            {
                network = new NetworkStatEntry(name.ToString());
                interfaces.Add(network);
                added = true;
            }

            network.Live = true;

            network.RxBytes = Int64.TryParse(span[range[1]], out var rxBytes) ? rxBytes : 0;
            network.RxPackets = Int64.TryParse(span[range[2]], out var rxPackets) ? rxPackets : 0;
            network.RxErrors = Int64.TryParse(span[range[3]], out var rxErrors) ? rxErrors : 0;
            network.RxDropped = Int64.TryParse(span[range[4]], out var rxDropped) ? rxDropped : 0;
            network.RxFifo = Int64.TryParse(span[range[5]], out var rxFifo) ? rxFifo : 0;
            network.RxFrame = Int64.TryParse(span[range[6]], out var rxFrame) ? rxFrame : 0;
            network.RxCompressed = Int64.TryParse(span[range[7]], out var rxCompressed) ? rxCompressed : 0;
            network.RxMulticast = Int64.TryParse(span[range[8]], out var rxMulticast) ? rxMulticast : 0;
            network.TxBytes = Int64.TryParse(span[range[9]], out var txBytes) ? txBytes : 0;
            network.TxPackets = Int64.TryParse(span[range[10]], out var txPackets) ? txPackets : 0;
            network.TxErrors = Int64.TryParse(span[range[11]], out var txErrors) ? txErrors : 0;
            network.TxDropped = Int64.TryParse(span[range[12]], out var txDropped) ? txDropped : 0;
            network.TxFifo = Int64.TryParse(span[range[13]], out var txFifo) ? txFifo : 0;
            network.TxCollisions = Int64.TryParse(span[range[14]], out var txCollisions) ? txCollisions : 0;
            network.TxCarrier = Int64.TryParse(span[range[15]], out var txCarrier) ? txCarrier : 0;
            network.TxCompressed = Int64.TryParse(span[range[16]], out var txCompressed) ? txCompressed : 0;
        }

        for (var i = interfaces.Count - 1; i >= 0; i--)
        {
            if (!interfaces[i].Live)
            {
                interfaces.RemoveAt(i);
            }
        }

        if (added)
        {
            interfaces.Sort(static (x, y) => String.Compare(x.Interface, y.Interface, StringComparison.Ordinal));
        }

        UpdateAt = DateTime.Now;

        return true;
    }
}
