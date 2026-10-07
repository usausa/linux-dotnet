namespace LinuxDotNet.SystemInfo;

using System.Text;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class WirelessStatEntry
{
    internal bool Live { get; set; }

    // Name in /proc/net/wireless to find the entry without creating a string
    internal byte[] RawName { get; }

    public string Interface { get; }

    public int Status { get; internal set; }

    public double LinkQuality { get; internal set; }

    // -30 dBm: Very strong (excellent)
    // -50 dBm: Strong(good)
    // -70 dBm: Weak(usable)
    // -90 dBm: Very weak(unstable)
    // -100 dBm Almost unusable
    public double SignalLevel { get; internal set; }

    public double NoiseLevel { get; internal set; }

    public ulong DiscardedNetworkId { get; internal set; }

    public ulong DiscardedCrypt { get; internal set; }

    public ulong DiscardedFragment { get; internal set; }

    public ulong DiscardedRetry { get; internal set; }

    public ulong DiscardedMisc { get; internal set; }

    public ulong MissedBeacon { get; internal set; }

    internal WirelessStatEntry(ReadOnlySpan<byte> interfaceName)
    {
        RawName = interfaceName.ToArray();
        Interface = Encoding.UTF8.GetString(interfaceName);
    }
}

public sealed class WirelessStat : IDisposable
{
    private readonly KernelFile file;

    private readonly List<WirelessStatEntry> interfaces = [];

    private bool disposed;

    public DateTime UpdateAt { get; internal set; }

    public IReadOnlyList<WirelessStatEntry> Interfaces => interfaces;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private WirelessStat(KernelFile file)
    {
        this.file = file;
    }

    internal static WirelessStat Create()
    {
        var instance = new WirelessStat(new KernelFile("/proc/net/wireless"));
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

        foreach (var wireless in interfaces)
        {
            wireless.Live = false;
        }

        var added = false;
        var remaining = file.Content;
        _ = TryReadLine(ref remaining, out _);
        _ = TryReadLine(ref remaining, out _);
        while (TryReadLine(ref remaining, out var line))
        {
            // The name, status, link, level, noise and the counters (11 or more tokens)
            var name = NextToken(ref line).TrimEnd((byte)':');
            var status = NextToken(ref line);
            var link = NextToken(ref line);
            var level = NextToken(ref line);
            var noise = NextToken(ref line);
            var discardedNetworkId = NextToken(ref line);
            var discardedCrypt = NextToken(ref line);
            var discardedFragment = NextToken(ref line);
            var discardedRetry = NextToken(ref line);
            var discardedMisc = NextToken(ref line);
            var missedBeacon = NextToken(ref line);
            if (missedBeacon.IsEmpty)
            {
                continue;
            }

            var wireless = default(WirelessStatEntry);
            foreach (var item in interfaces)
            {
                if (name.SequenceEqual(item.RawName))
                {
                    wireless = item;
                    break;
                }
            }

            if (wireless == null)
            {
                wireless = new WirelessStatEntry(name);
                interfaces.Add(wireless);
                added = true;
            }

            wireless.Live = true;

            wireless.Status = ParseStatus(status);
            wireless.LinkQuality = ParseDouble(link.TrimEnd((byte)'.'));
            wireless.SignalLevel = ParseDouble(level.TrimEnd((byte)'.'));
            wireless.NoiseLevel = ParseDouble(noise.TrimEnd((byte)'.'));
            wireless.DiscardedNetworkId = ParseUInt64(discardedNetworkId);
            wireless.DiscardedCrypt = ParseUInt64(discardedCrypt);
            wireless.DiscardedFragment = ParseUInt64(discardedFragment);
            wireless.DiscardedRetry = ParseUInt64(discardedRetry);
            wireless.DiscardedMisc = ParseUInt64(discardedMisc);
            wireless.MissedBeacon = ParseUInt64(missedBeacon);
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

    // The same as Int32.TryParse with NumberStyles.HexNumber: up to 32 bits as a two's complement value, otherwise 0
    private static int ParseStatus(ReadOnlySpan<byte> span)
    {
        var value = ParseHex(span);
        return value <= UInt32.MaxValue ? unchecked((int)value) : 0;
    }
}
