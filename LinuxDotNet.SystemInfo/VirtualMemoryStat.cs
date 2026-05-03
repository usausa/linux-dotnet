namespace LinuxDotNet.SystemInfo;

using System;

public sealed class VirtualMemoryStat
{
    public DateTime UpdateAt { get; private set; }

    // Page

    public ulong PageIn { get; internal set; }

    public ulong PageOut { get; internal set; }

    // Swap

    public ulong SwapIn { get; internal set; }

    public ulong SwapOut { get; internal set; }

    // Fault

    public ulong PageFaults { get; internal set; }

    public ulong MajorPageFaults { get; internal set; }

    // Steal

    public ulong StealKernel { get; internal set; }

    public ulong StealDirect { get; internal set; }

    // Scan

    public ulong ScanKernel { get; internal set; }

    public ulong ScanDirect { get; internal set; }

    // OOM

    public ulong OutOfMemoryKiller { get; internal set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal VirtualMemoryStat()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    // ReSharper disable StringLiteralTypo
    public bool Update()
    {
        using var reader = new StreamReader("/proc/vmstat");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            if (span.StartsWith("pgpgin"))
            {
                PageIn = ExtractUInt64(span);
            }
            else if (span.StartsWith("pgpgout"))
            {
                PageOut = ExtractUInt64(span);
            }
            else if (span.StartsWith("pswpin"))
            {
                SwapIn = ExtractUInt64(span);
            }
            else if (span.StartsWith("pswpout"))
            {
                SwapOut = ExtractUInt64(span);
            }
            else if (span.StartsWith("pgfault"))
            {
                PageFaults = ExtractUInt64(span);
            }
            else if (span.StartsWith("pgmajfault"))
            {
                MajorPageFaults = ExtractUInt64(span);
            }
            else if (span.StartsWith("pgsteal_kswapd"))
            {
                StealKernel = ExtractUInt64(span);
            }
            else if (span.StartsWith("pgsteal_direct"))
            {
                StealDirect = ExtractUInt64(span);
            }
            else if (span.StartsWith("pgscan_kswapd"))
            {
                ScanKernel = ExtractUInt64(span);
            }
            else if (span.StartsWith("pgscan_direct"))
            {
                ScanDirect = ExtractUInt64(span);
            }
            else if (span.StartsWith("oom_kill"))
            {
                OutOfMemoryKiller = ExtractUInt64(span);
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static ulong ExtractUInt64(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && UInt64.TryParse(span[range[1]], out var result) ? result : 0;
    }
}
