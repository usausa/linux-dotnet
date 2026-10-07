namespace LinuxDotNet.SystemInfo;

using System;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class VirtualMemoryStat : IDisposable
{
    private readonly KernelFile file;

    private bool disposed;

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

    private VirtualMemoryStat(KernelFile file)
    {
        this.file = file;
    }

    internal static VirtualMemoryStat Create()
    {
        var instance = new VirtualMemoryStat(new KernelFile("/proc/vmstat"));
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
            var value = NextToken(ref line);
            if (value.IsEmpty)
            {
                continue;
            }

            if (key.SequenceEqual("pgpgin"u8))
            {
                PageIn = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pgpgout"u8))
            {
                PageOut = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pswpin"u8))
            {
                SwapIn = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pswpout"u8))
            {
                SwapOut = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pgfault"u8))
            {
                PageFaults = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pgmajfault"u8))
            {
                MajorPageFaults = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pgsteal_kswapd"u8))
            {
                StealKernel = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pgsteal_direct"u8))
            {
                StealDirect = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pgscan_kswapd"u8))
            {
                ScanKernel = ParseUInt64(value);
            }
            else if (key.SequenceEqual("pgscan_direct"u8))
            {
                ScanDirect = ParseUInt64(value);
            }
            else if (key.SequenceEqual("oom_kill"u8))
            {
                OutOfMemoryKiller = ParseUInt64(value);
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo
}
