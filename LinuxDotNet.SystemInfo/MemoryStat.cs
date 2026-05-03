namespace LinuxDotNet.SystemInfo;

using System;

public sealed class MemoryStat
{
    public DateTime UpdateAt { get; private set; }

    public ulong MemoryTotal { get; private set; }

    public ulong MemoryAvailable { get; private set; }

    public ulong MemoryFree { get; private set; }

    public ulong Buffers { get; private set; }

    public ulong Cached { get; private set; }

    public ulong SwapCached { get; private set; }

    public ulong ActiveAnonymous { get; private set; }

    public ulong InactiveAnonymous { get; private set; }

    public ulong ActiveFile { get; private set; }

    public ulong InactiveFile { get; private set; }

    public ulong Unevictable { get; private set; }

    public ulong MemoryLocked { get; private set; }

    public ulong SwapTotal { get; private set; }

    public ulong SwapFree { get; private set; }

    public ulong Dirty { get; private set; }

    public ulong Writeback { get; private set; }

    public ulong AnonymousPages { get; private set; }

    public ulong Mapped { get; private set; }

    public ulong SharedMemory { get; private set; }

    public ulong KernelReclaimable { get; private set; }

    public ulong SlabTotal { get; private set; }

    public ulong SlabReclaimable { get; private set; }

    public ulong SlabUnreclaimable { get; private set; }

    public ulong KernelStack { get; private set; }

    public ulong PageTables { get; private set; }

    public ulong CommitLimit { get; private set; }

    public ulong CommittedAddressSpace { get; private set; }

    public ulong HardwareCorrupted { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal MemoryStat()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        using var reader = new StreamReader("/proc/meminfo");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            // ReSharper disable StringLiteralTypo
            if (span.StartsWith("MemTotal:"))
            {
                MemoryTotal = ExtractUInt64(span);
            }
            else if (span.StartsWith("MemAvailable:"))
            {
                MemoryAvailable = ExtractUInt64(span);
            }
            else if (span.StartsWith("MemFree:"))
            {
                MemoryFree = ExtractUInt64(span);
            }
            else if (span.StartsWith("Buffers:"))
            {
                Buffers = ExtractUInt64(span);
            }
            else if (span.StartsWith("Cached:"))
            {
                Cached = ExtractUInt64(span);
            }
            else if (span.StartsWith("SwapCached:"))
            {
                SwapCached = ExtractUInt64(span);
            }
            else if (span.StartsWith("Active(anon):"))
            {
                ActiveAnonymous = ExtractUInt64(span);
            }
            else if (span.StartsWith("Inactive(anon):"))
            {
                InactiveAnonymous = ExtractUInt64(span);
            }
            else if (span.StartsWith("Active(file):"))
            {
                ActiveFile = ExtractUInt64(span);
            }
            else if (span.StartsWith("Inactive(file):"))
            {
                InactiveFile = ExtractUInt64(span);
            }
            else if (span.StartsWith("Unevictable:"))
            {
                Unevictable = ExtractUInt64(span);
            }
            else if (span.StartsWith("Mlocked:"))
            {
                MemoryLocked = ExtractUInt64(span);
            }
            else if (span.StartsWith("SwapTotal:"))
            {
                SwapTotal = ExtractUInt64(span);
            }
            else if (span.StartsWith("SwapFree:"))
            {
                SwapFree = ExtractUInt64(span);
            }
            else if (span.StartsWith("Dirty:"))
            {
                Dirty = ExtractUInt64(span);
            }
            else if (span.StartsWith("Writeback:"))
            {
                Writeback = ExtractUInt64(span);
            }
            else if (span.StartsWith("AnonPages:"))
            {
                AnonymousPages = ExtractUInt64(span);
            }
            else if (span.StartsWith("Mapped:"))
            {
                Mapped = ExtractUInt64(span);
            }
            else if (span.StartsWith("Shmem:"))
            {
                SharedMemory = ExtractUInt64(span);
            }
            else if (span.StartsWith("KReclaimable:"))
            {
                KernelReclaimable = ExtractUInt64(span);
            }
            else if (span.StartsWith("Slab:"))
            {
                SlabTotal = ExtractUInt64(span);
            }
            else if (span.StartsWith("SReclaimable:"))
            {
                SlabReclaimable = ExtractUInt64(span);
            }
            else if (span.StartsWith("SUnreclaim:"))
            {
                SlabUnreclaimable = ExtractUInt64(span);
            }
            else if (span.StartsWith("KernelStack:"))
            {
                KernelStack = ExtractUInt64(span);
            }
            else if (span.StartsWith("PageTables:"))
            {
                PageTables = ExtractUInt64(span);
            }
            else if (span.StartsWith("CommitLimit:"))
            {
                CommitLimit = ExtractUInt64(span);
            }
            else if (span.StartsWith("Committed_AS:"))
            {
                CommittedAddressSpace = ExtractUInt64(span);
            }
            else if (span.StartsWith("HardwareCorrupted:"))
            {
                HardwareCorrupted = ExtractUInt64(span);
            }
            // ReSharper restore StringLiteralTypo
        }

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static ulong ExtractUInt64(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && UInt64.TryParse(span[range[1]], out var result) ? result : 0;
    }
}
