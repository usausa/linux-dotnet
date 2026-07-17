namespace LinuxDotNet.SystemInfo;

using System.Globalization;

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

    public ulong DiscardedNetworkId { get; internal set; }

    public ulong DiscardedCrypt { get; internal set; }

    public ulong DiscardedFragment { get; internal set; }

    public ulong DiscardedRetry { get; internal set; }

    public ulong DiscardedMisc { get; internal set; }

    public ulong MissedBeacon { get; internal set; }

    internal WirelessStatEntry(string interfaceName)
    {
        Interface = interfaceName;
    }
}

public sealed class WirelessStat
{
    private readonly List<WirelessStatEntry> interfaces = [];

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

        var added = false;
        try
        {
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
                    added = true;
                }

                wireless.Live = true;

                wireless.Status = Int32.TryParse(span[range[1]], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var status) ? status : 0;
                wireless.LinkQuality = Double.TryParse(span[range[2]].TrimEnd('.'), CultureInfo.InvariantCulture, out var qualityLink) ? qualityLink : 0;
                wireless.SignalLevel = Double.TryParse(span[range[3]].TrimEnd('.'), CultureInfo.InvariantCulture, out var qualityLevel) ? qualityLevel : 0;
                wireless.NoiseLevel = Double.TryParse(span[range[4]].TrimEnd('.'), CultureInfo.InvariantCulture, out var qualityNoise) ? qualityNoise : 0;
                wireless.DiscardedNetworkId = UInt64.TryParse(span[range[5]], CultureInfo.InvariantCulture, out var discardedNetworkId) ? discardedNetworkId : 0;
                wireless.DiscardedCrypt = UInt64.TryParse(span[range[6]], CultureInfo.InvariantCulture, out var discardedCrypt) ? discardedCrypt : 0;
                wireless.DiscardedFragment = UInt64.TryParse(span[range[7]], CultureInfo.InvariantCulture, out var discardedFragment) ? discardedFragment : 0;
                wireless.DiscardedRetry = UInt64.TryParse(span[range[8]], CultureInfo.InvariantCulture, out var discardedRetry) ? discardedRetry : 0;
                wireless.DiscardedMisc = UInt64.TryParse(span[range[9]], CultureInfo.InvariantCulture, out var discardedMisc) ? discardedMisc : 0;
                wireless.MissedBeacon = UInt64.TryParse(span[range[10]], CultureInfo.InvariantCulture, out var missedBeacon) ? missedBeacon : 0;
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
