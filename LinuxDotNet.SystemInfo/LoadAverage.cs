namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

public sealed class LoadAverage
{
    public DateTime UpdateAt { get; private set; }

    public double Average1 { get; private set; }

    public double Average5 { get; private set; }

    public double Average15 { get; private set; }

    internal LoadAverage()
    {
        Update();
    }

    // ReSharper disable StringLiteralTypo
    public bool Update()
    {
        var span = File.ReadAllText("/proc/loadavg").AsSpan();
        var range = (Span<Range>)stackalloc Range[5];
        span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries);
        Average1 = Double.TryParse(span[range[0]], CultureInfo.InvariantCulture, out var v1) ? v1 : 0;
        Average5 = Double.TryParse(span[range[1]], CultureInfo.InvariantCulture, out var v5) ? v5 : 0;
        Average15 = Double.TryParse(span[range[2]], CultureInfo.InvariantCulture, out var v15) ? v15 : 0;

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo
}
