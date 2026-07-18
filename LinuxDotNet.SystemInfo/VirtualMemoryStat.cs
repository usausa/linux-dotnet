namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

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
        var range = (Span<Range>)stackalloc Range[3];
        using var reader = new StreamReader("/proc/vmstat");
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 2)
            {
                continue;
            }

            var value = span[range[1]];
            switch (span[range[0]])
            {
                case "pgpgin":
                    PageIn = ParseUInt64(value);
                    break;
                case "pgpgout":
                    PageOut = ParseUInt64(value);
                    break;
                case "pswpin":
                    SwapIn = ParseUInt64(value);
                    break;
                case "pswpout":
                    SwapOut = ParseUInt64(value);
                    break;
                case "pgfault":
                    PageFaults = ParseUInt64(value);
                    break;
                case "pgmajfault":
                    MajorPageFaults = ParseUInt64(value);
                    break;
                case "pgsteal_kswapd":
                    StealKernel = ParseUInt64(value);
                    break;
                case "pgsteal_direct":
                    StealDirect = ParseUInt64(value);
                    break;
                case "pgscan_kswapd":
                    ScanKernel = ParseUInt64(value);
                    break;
                case "pgscan_direct":
                    ScanDirect = ParseUInt64(value);
                    break;
                case "oom_kill":
                    OutOfMemoryKiller = ParseUInt64(value);
                    break;
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static ulong ParseUInt64(ReadOnlySpan<char> span) =>
        UInt64.TryParse(span, CultureInfo.InvariantCulture, out var result) ? result : 0;
}
