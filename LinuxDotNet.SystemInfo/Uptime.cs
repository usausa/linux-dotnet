namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

public sealed class Uptime
{
    public DateTime UpdateAt { get; private set; }

    public TimeSpan Elapsed { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal Uptime()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        var span = File.ReadAllText("/proc/uptime").AsSpan();
        var range = (Span<Range>)stackalloc Range[2];
        span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries);
        var second = Double.Parse(span[range[0]], CultureInfo.InvariantCulture);
        Elapsed = TimeSpan.FromSeconds(second);

        UpdateAt = DateTime.Now;

        return true;
    }
}
