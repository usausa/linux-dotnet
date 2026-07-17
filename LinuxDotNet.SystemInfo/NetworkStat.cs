namespace LinuxDotNet.SystemInfo;

using System.Globalization;

public sealed class NetworkStatEntry
{
    internal bool Live { get; set; }

    public string Interface { get; }

    public ulong RxBytes { get; internal set; }

    public ulong RxPackets { get; internal set; }

    public ulong RxErrors { get; internal set; }

    public ulong RxDropped { get; internal set; }

    public ulong RxFifo { get; internal set; }

    public ulong RxFrame { get; internal set; }

    public ulong RxCompressed { get; internal set; }

    public ulong RxMulticast { get; internal set; }

    public ulong TxBytes { get; internal set; }

    public ulong TxPackets { get; internal set; }

    public ulong TxErrors { get; internal set; }

    public ulong TxDropped { get; internal set; }

    public ulong TxFifo { get; internal set; }

    public ulong TxCollisions { get; internal set; }

    public ulong TxCarrier { get; internal set; }

    public ulong TxCompressed { get; internal set; }

    internal NetworkStatEntry(string interfaceName)
    {
        Interface = interfaceName;
    }
}

public sealed class NetworkStat
{
    private readonly List<NetworkStatEntry> interfaces = [];

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

        var added = false;
        try
        {
            var range = (Span<Range>)stackalloc Range[18];
            using var reader = new StreamReader("/proc/net/dev");
            reader.ReadLine();
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

                network.RxBytes = UInt64.TryParse(span[range[1]], CultureInfo.InvariantCulture, out var rxBytes) ? rxBytes : 0;
                network.RxPackets = UInt64.TryParse(span[range[2]], CultureInfo.InvariantCulture, out var rxPackets) ? rxPackets : 0;
                network.RxErrors = UInt64.TryParse(span[range[3]], CultureInfo.InvariantCulture, out var rxErrors) ? rxErrors : 0;
                network.RxDropped = UInt64.TryParse(span[range[4]], CultureInfo.InvariantCulture, out var rxDropped) ? rxDropped : 0;
                network.RxFifo = UInt64.TryParse(span[range[5]], CultureInfo.InvariantCulture, out var rxFifo) ? rxFifo : 0;
                network.RxFrame = UInt64.TryParse(span[range[6]], CultureInfo.InvariantCulture, out var rxFrame) ? rxFrame : 0;
                network.RxCompressed = UInt64.TryParse(span[range[7]], CultureInfo.InvariantCulture, out var rxCompressed) ? rxCompressed : 0;
                network.RxMulticast = UInt64.TryParse(span[range[8]], CultureInfo.InvariantCulture, out var rxMulticast) ? rxMulticast : 0;
                network.TxBytes = UInt64.TryParse(span[range[9]], CultureInfo.InvariantCulture, out var txBytes) ? txBytes : 0;
                network.TxPackets = UInt64.TryParse(span[range[10]], CultureInfo.InvariantCulture, out var txPackets) ? txPackets : 0;
                network.TxErrors = UInt64.TryParse(span[range[11]], CultureInfo.InvariantCulture, out var txErrors) ? txErrors : 0;
                network.TxDropped = UInt64.TryParse(span[range[12]], CultureInfo.InvariantCulture, out var txDropped) ? txDropped : 0;
                network.TxFifo = UInt64.TryParse(span[range[13]], CultureInfo.InvariantCulture, out var txFifo) ? txFifo : 0;
                network.TxCollisions = UInt64.TryParse(span[range[14]], CultureInfo.InvariantCulture, out var txCollisions) ? txCollisions : 0;
                network.TxCarrier = UInt64.TryParse(span[range[15]], CultureInfo.InvariantCulture, out var txCarrier) ? txCarrier : 0;
                network.TxCompressed = UInt64.TryParse(span[range[16]], CultureInfo.InvariantCulture, out var txCompressed) ? txCompressed : 0;
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
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
