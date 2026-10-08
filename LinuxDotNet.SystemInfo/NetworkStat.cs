namespace LinuxDotNet.SystemInfo;

using System.Text;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class NetworkStatEntry
{
    internal bool Live { get; set; }

    internal byte[] RawName { get; }

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

    internal NetworkStatEntry(ReadOnlySpan<byte> interfaceName)
    {
        RawName = interfaceName.ToArray();
        Interface = Encoding.UTF8.GetString(interfaceName);
    }
}

public sealed class NetworkStat : IDisposable
{
    private const int ValueCount = 16;

    private readonly KernelFile file;

    private readonly List<NetworkStatEntry> interfaces = [];

    private bool disposed;

    public DateTime UpdateAt { get; internal set; }

    public IReadOnlyList<NetworkStatEntry> Interfaces => interfaces;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private NetworkStat(KernelFile file)
    {
        this.file = file;
    }

    internal static NetworkStat Create()
    {
        var instance = new NetworkStat(new KernelFile("/proc/net/dev"));
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

        foreach (var network in interfaces)
        {
            network.Live = false;
        }

        var values = (Span<ulong>)stackalloc ulong[ValueCount];
        var added = false;
        var remaining = file.Content;
        _ = TryReadLine(ref remaining, out _);
        while (TryReadLine(ref remaining, out var line))
        {
            // The name and the counters (17 or more tokens, the second header line has 16)
            var name = NextToken(ref line).TrimEnd((byte)':');
            if (!TryParseValues(ref line, values))
            {
                continue;
            }

            var network = default(NetworkStatEntry);
            foreach (var item in interfaces)
            {
                if (name.SequenceEqual(item.RawName))
                {
                    network = item;
                    break;
                }
            }

            if (network == null)
            {
                network = new NetworkStatEntry(name);
                interfaces.Add(network);
                added = true;
            }

            network.Live = true;

            network.RxBytes = values[0];
            network.RxPackets = values[1];
            network.RxErrors = values[2];
            network.RxDropped = values[3];
            network.RxFifo = values[4];
            network.RxFrame = values[5];
            network.RxCompressed = values[6];
            network.RxMulticast = values[7];
            network.TxBytes = values[8];
            network.TxPackets = values[9];
            network.TxErrors = values[10];
            network.TxDropped = values[11];
            network.TxFifo = values[12];
            network.TxCollisions = values[13];
            network.TxCarrier = values[14];
            network.TxCompressed = values[15];
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
