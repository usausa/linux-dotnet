namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

public sealed class UptimeInfo
{
    public DateTime UpdateAt { get; private set; }

    public TimeSpan Uptime { get; private set; }

    internal UptimeInfo()
    {
        Update();
    }

    public bool Update()
    {
        var span = File.ReadAllText("/proc/uptime").AsSpan();
        var range = (Span<Range>)stackalloc Range[2];
        span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries);
        var second = Double.Parse(span[range[0]], CultureInfo.InvariantCulture);
        Uptime = TimeSpan.FromSeconds(second);

        UpdateAt = DateTime.Now;

        return true;
    }
}
