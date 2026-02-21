namespace LinuxDotNet.SystemInfo;

public sealed class WirelessStatEntry
{
    internal bool Live { get; set; }

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

    public long DiscardedNetworkId { get; internal set; }

    public long DiscardedCrypt { get; internal set; }

    public long DiscardedFragment { get; internal set; }

    public long DiscardedRetry { get; internal set; }

    public long DiscardedMisc { get; internal set; }

    public long MissedBeacon { get; internal set; }

    internal WirelessStatEntry(string interfaceName)
    {
        Interface = interfaceName;
    }
}

public sealed class WirelessStat
{
    private readonly List<WirelessStatEntry> interfaces = new();

    public DateTime UpdateAt { get; internal set; }

    public IReadOnlyList<WirelessStatEntry> Interfaces => interfaces;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal WirelessStat()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        foreach (var wireless in interfaces)
        {
            wireless.Live = false;
        }

        var range = (Span<Range>)stackalloc Range[12];
        using var reader = new StreamReader("/proc/net/wireless");
        reader.ReadLine();
        reader.ReadLine();
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 11)
            {
                continue;
            }

            var name = span[range[0]].TrimEnd(':');
            var wireless = default(WirelessStatEntry);
            foreach (var item in interfaces)
            {
                if (item.Interface == name)
                {
                    wireless = item;
                    break;
                }
            }

            if (wireless == null)
            {
                wireless = new WirelessStatEntry(name.ToString());
                interfaces.Add(wireless);
            }

            wireless.Live = true;

            wireless.Status = Int32.TryParse(span[range[1]], System.Globalization.NumberStyles.HexNumber, null, out var status) ? status : 0;
            wireless.LinkQuality = Double.TryParse(span[range[2]].TrimEnd('.'), out var qualityLink) ? qualityLink : 0;
            wireless.SignalLevel = Double.TryParse(span[range[3]].TrimEnd('.'), out var qualityLevel) ? qualityLevel : 0;
            wireless.NoiseLevel = Double.TryParse(span[range[4]].TrimEnd('.'), out var qualityNoise) ? qualityNoise : 0;
            wireless.DiscardedNetworkId = Int64.TryParse(span[range[5]], out var discardedNetworkId) ? discardedNetworkId : 0;
            wireless.DiscardedCrypt = Int64.TryParse(span[range[6]], out var discardedCrypt) ? discardedCrypt : 0;
            wireless.DiscardedFragment = Int64.TryParse(span[range[7]], out var discardedFragment) ? discardedFragment : 0;
            wireless.DiscardedRetry = Int64.TryParse(span[range[8]], out var discardedRetry) ? discardedRetry : 0;
            wireless.DiscardedMisc = Int64.TryParse(span[range[9]], out var discardedMisc) ? discardedMisc : 0;
            wireless.MissedBeacon = Int64.TryParse(span[range[10]], out var missedBeacon) ? missedBeacon : 0;
        }

        for (var i = interfaces.Count - 1; i >= 0; i--)
        {
            if (!interfaces[i].Live)
            {
                interfaces.RemoveAt(i);
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
}
