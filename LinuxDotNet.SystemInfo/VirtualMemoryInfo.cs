namespace LinuxDotNet.SystemInfo;

using System;

public class VirtualMemoryInfo
{
    public DateTime UpdateAt { get; private set; }

    // Total
    public long PageIn { get; internal set; }

    // Total
    public long PageOut { get; internal set; }

    // Total
    public long SwapIn { get; internal set; }

    // Total
    public long SwapOut { get; internal set; }

    // Total
    public long PageFault { get; internal set; }

    // Total
    public long MajorPageFault { get; internal set; }

    // Total
    public long OutOfMemoryKiller { get; internal set; }

    internal VirtualMemoryInfo()
    {
        Update();
    }

    // ReSharper disable StringLiteralTypo
    public bool Update()
    {
        using var reader = new StreamReader("/proc/vmstat");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            if (span.StartsWith("pgpgin"))
            {
                PageIn = ExtractInt64(span);
            }
            else if (span.StartsWith("pgpgout"))
            {
                PageOut = ExtractInt64(span);
            }
            else if (span.StartsWith("pswpin"))
            {
                SwapIn = ExtractInt64(span);
            }
            else if (span.StartsWith("pswpout"))
            {
                SwapOut = ExtractInt64(span);
            }
            else if (span.StartsWith("pgfault"))
            {
                PageFault = ExtractInt64(span);
            }
            else if (span.StartsWith("pgmajfault"))
            {
                MajorPageFault = ExtractInt64(span);
            }
            else if (span.StartsWith("oom_kill"))
            {
                OutOfMemoryKiller = ExtractInt64(span);
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo

    private static long ExtractInt64(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && Int64.TryParse(span[range[1]], out var result) ? result : 0;
    }
}
