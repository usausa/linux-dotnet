namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

public sealed class CpuStat
{
    public string Name { get; }

    public ulong User { get; internal set; }

    public ulong Nice { get; internal set; }

    public ulong System { get; internal set; }

    public ulong Idle { get; internal set; }

    public ulong IoWait { get; internal set; }

    public ulong Irq { get; internal set; }

    public ulong SoftIrq { get; internal set; }

    public ulong Steal { get; internal set; }

    public ulong Guest { get; internal set; }

    public ulong GuestNice { get; internal set; }

    internal CpuStat(string name)
    {
        Name = name;
    }
}

public sealed class SystemStat
{
    private readonly List<CpuStat> cpuCores = [];

    public DateTime UpdateAt { get; private set; }

    public CpuStat CpuTotal { get; } = new("total");

    public IReadOnlyList<CpuStat> CpuCores => cpuCores;

    // Total
    public ulong Interrupt { get; private set; }

    // Total
    public ulong ContextSwitch { get; private set; }

    // Total
    public ulong Forks { get; private set; }

    public int RunnableTasks { get; private set; }

    public int BlockedTasks { get; private set; }

    // Total
    public ulong SoftIrq { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal SystemStat()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    // ReSharper disable StringLiteralTypo
    public bool Update()
    {
        using var reader = new StreamReader("/proc/stat");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();

            if (span.StartsWith("cpu"))
            {
                UpdateCpuValue(span);
            }
            else if (span.StartsWith("intr"))
            {
                Interrupt = ExtractUInt64(span);
            }
            else if (span.StartsWith("ctxt"))
            {
                ContextSwitch = ExtractUInt64(span);
            }
            else if (span.StartsWith("processes"))
            {
                Forks = ExtractUInt64(span);
            }
            else if (span.StartsWith("procs_running"))
            {
                RunnableTasks = ExtractInt32(span);
            }
            else if (span.StartsWith("procs_blocked"))
            {
                BlockedTasks = ExtractInt32(span);
            }
            else if (span.StartsWith("softirq"))
            {
                SoftIrq = ExtractUInt64(span);
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo

    private void UpdateCpuValue(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[12];
        span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries);

        var stat = span[range[0]] is "cpu" ? CpuTotal : FindCpu(span[range[0]]);

        stat.User = UInt64.TryParse(span[range[1]], CultureInfo.InvariantCulture, out var value) ? value : 0;
        stat.Nice = UInt64.TryParse(span[range[2]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.System = UInt64.TryParse(span[range[3]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.Idle = UInt64.TryParse(span[range[4]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.IoWait = UInt64.TryParse(span[range[5]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.Irq = UInt64.TryParse(span[range[6]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.SoftIrq = UInt64.TryParse(span[range[7]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.Steal = UInt64.TryParse(span[range[8]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.Guest = UInt64.TryParse(span[range[9]], CultureInfo.InvariantCulture, out value) ? value : 0;
        stat.GuestNice = UInt64.TryParse(span[range[10]], CultureInfo.InvariantCulture, out value) ? value : 0;
    }

    private CpuStat FindCpu(ReadOnlySpan<char> name)
    {
        foreach (var core in cpuCores)
        {
            if (core.Name.AsSpan().Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return core;
            }
        }

        var cpu = new CpuStat(name.ToString());
        cpuCores.Add(cpu);
        return cpu;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static ulong ExtractUInt64(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && UInt64.TryParse(span[range[1]], CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static int ExtractInt32(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && Int32.TryParse(span[range[1]], CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
