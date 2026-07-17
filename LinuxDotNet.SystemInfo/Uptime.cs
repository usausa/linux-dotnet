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
        if (!FileHelper.TryReadText("/proc/uptime", out var text))
        {
            return false;
        }

        var span = text.AsSpan();
        var range = (Span<Range>)stackalloc Range[2];
        span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries);
        Elapsed = Double.TryParse(span[range[0]], CultureInfo.InvariantCulture, out var second) ? TimeSpan.FromSeconds(second) : TimeSpan.Zero;

        UpdateAt = DateTime.Now;

        return true;
    }
}
