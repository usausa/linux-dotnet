namespace LinuxDotNet.SystemInfo;

using System;
using System.Text;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class CpuStat
{
    internal byte[] RawName { get; }

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

    internal CpuStat(ReadOnlySpan<byte> rawName)
        : this(Encoding.UTF8.GetString(rawName), rawName)
    {
    }

    internal CpuStat(string name, ReadOnlySpan<byte> rawName)
    {
        Name = name;
        RawName = rawName.ToArray();
    }
}

public sealed class SystemStat : IDisposable
{
    private readonly KernelFile file;

    private readonly List<CpuStat> cpuCores = [];

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public CpuStat CpuTotal { get; } = new("total", "cpu"u8);

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

    private SystemStat(KernelFile file)
    {
        this.file = file;
    }

    internal static SystemStat Create()
    {
        var instance = new SystemStat(new KernelFile("/proc/stat"));
        instance.Update();
        return instance;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        file.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    // ReSharper disable StringLiteralTypo
    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!file.Read())
        {
            return false;
        }

        var remaining = file.Content;
        while (TryReadLine(ref remaining, out var line))
        {
            var key = NextToken(ref line);
            if (key.SequenceEqual("cpu"u8))
            {
                UpdateCpuValue(CpuTotal, line);
            }
            else if (key.StartsWith("cpu"u8))
            {
                UpdateCpuValue(FindCpu(key), line);
            }
            else if (key.SequenceEqual("intr"u8))
            {
                Interrupt = ParseUInt64(NextToken(ref line));
            }
            else if (key.SequenceEqual("ctxt"u8))
            {
                ContextSwitch = ParseUInt64(NextToken(ref line));
            }
            else if (key.SequenceEqual("processes"u8))
            {
                Forks = ParseUInt64(NextToken(ref line));
            }
            else if (key.SequenceEqual("procs_running"u8))
            {
                RunnableTasks = ParseInt32(NextToken(ref line));
            }
            else if (key.SequenceEqual("procs_blocked"u8))
            {
                BlockedTasks = ParseInt32(NextToken(ref line));
            }
            else if (key.SequenceEqual("softirq"u8))
            {
                SoftIrq = ParseUInt64(NextToken(ref line));
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo

    private static void UpdateCpuValue(CpuStat stat, ReadOnlySpan<byte> values)
    {
        stat.User = ParseUInt64(NextToken(ref values));
        stat.Nice = ParseUInt64(NextToken(ref values));
        stat.System = ParseUInt64(NextToken(ref values));
        stat.Idle = ParseUInt64(NextToken(ref values));
        stat.IoWait = ParseUInt64(NextToken(ref values));
        stat.Irq = ParseUInt64(NextToken(ref values));
        stat.SoftIrq = ParseUInt64(NextToken(ref values));
        stat.Steal = ParseUInt64(NextToken(ref values));
        stat.Guest = ParseUInt64(NextToken(ref values));
        stat.GuestNice = ParseUInt64(NextToken(ref values));
    }

    private CpuStat FindCpu(ReadOnlySpan<byte> name)
    {
        foreach (var core in cpuCores)
        {
            if (name.SequenceEqual(core.RawName))
            {
                return core;
            }
        }

        var cpu = new CpuStat(name);
        cpuCores.Add(cpu);
        return cpu;
    }
}
