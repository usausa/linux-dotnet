namespace LinuxDotNet.SystemInfo;

using System;

public class VirtualMemoryInfo
{
    public DateTime UpdateAt { get; private set; }

    // Page

    public long PageIn { get; internal set; }

    public long PageOut { get; internal set; }

    // Swap

    public long SwapIn { get; internal set; }

    public long SwapOut { get; internal set; }

    // Fault

    public long PageFaults { get; internal set; }

    public long MajorPageFaults { get; internal set; }

    // Steal

    public long StealKernel { get; internal set; }

    public long StealDirect { get; internal set; }

    // Scan

    public long ScanKernel { get; internal set; }

    public long ScanDirect { get; internal set; }

    // OOM

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
                PageFaults = ExtractInt64(span);
            }
            else if (span.StartsWith("pgmajfault"))
            {
                MajorPageFaults = ExtractInt64(span);
            }
            else if (span.StartsWith("pgsteal_kswapd"))
            {
                StealKernel = ExtractInt64(span);
            }
            else if (span.StartsWith("pgsteal_direct"))
            {
                StealDirect = ExtractInt64(span);
            }
            else if (span.StartsWith("pgscan_kswapd"))
            {
                ScanKernel = ExtractInt64(span);
            }
            else if (span.StartsWith("pgscan_direct"))
            {
                ScanDirect = ExtractInt64(span);
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
