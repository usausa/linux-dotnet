namespace LinuxDotNet.SystemInfo;

using System;

public sealed class MemoryStat
{
    public DateTime UpdateAt { get; private set; }

    public long MemoryTotal { get; private set; }

    public long MemoryAvailable { get; private set; }

    public long MemoryFree { get; private set; }

    public long Buffers { get; private set; }

    public long Cached { get; private set; }

    public long SwapCached { get; private set; }

    public long ActiveAnonymous { get; private set; }

    public long InactiveAnonymous { get; private set; }

    public long ActiveFile { get; private set; }

    public long InactiveFile { get; private set; }

    public long Unevictable { get; private set; }

    public long MemoryLocked { get; private set; }

    public long SwapTotal { get; private set; }

    public long SwapFree { get; private set; }

    public long Dirty { get; private set; }

    public long Writeback { get; private set; }

    public long AnonymousPages { get; private set; }

    public long Mapped { get; private set; }

    public long SharedMemory { get; private set; }

    public long KernelReclaimable { get; private set; }

    public long SlabTotal { get; private set; }

    public long SlabReclaimable { get; private set; }

    public long SlabUnreclaimable { get; private set; }

    public long KernelStack { get; private set; }

    public long PageTables { get; private set; }

    public long CommitLimit { get; private set; }

    public long CommittedAddressSpace { get; private set; }

    public long HardwareCorrupted { get; private set; }

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
            if (span.StartsWith("MemTotal"))
            {
                MemoryTotal = ExtractInt64(span);
            }
            else if (span.StartsWith("MemAvailable"))
            {
                MemoryAvailable = ExtractInt64(span);
            }
            else if (span.StartsWith("MemFree"))
            {
                MemoryFree = ExtractInt64(span);
            }
            else if (span.StartsWith("Buffers"))
            {
                Buffers = ExtractInt64(span);
            }
            else if (span.StartsWith("Cached"))
            {
                Cached = ExtractInt64(span);
            }
            else if (span.StartsWith("SwapCached"))
            {
                SwapCached = ExtractInt64(span);
            }
            else if (span.StartsWith("Active(anon)"))
            {
                ActiveAnonymous = ExtractInt64(span);
            }
            else if (span.StartsWith("Inactive(anon)"))
            {
                InactiveAnonymous = ExtractInt64(span);
            }
            else if (span.StartsWith("Active(file)"))
            {
                ActiveFile = ExtractInt64(span);
            }
            else if (span.StartsWith("Inactive(file)"))
            {
                InactiveFile = ExtractInt64(span);
            }
            else if (span.StartsWith("Unevictable"))
            {
                Unevictable = ExtractInt64(span);
            }
            else if (span.StartsWith("Mlocked"))
            {
                MemoryLocked = ExtractInt64(span);
            }
            else if (span.StartsWith("SwapTotal"))
            {
                SwapTotal = ExtractInt64(span);
            }
            else if (span.StartsWith("SwapFree"))
            {
                SwapFree = ExtractInt64(span);
            }
            else if (span.StartsWith("Dirty"))
            {
                Dirty = ExtractInt64(span);
            }
            else if (span.StartsWith("Writeback"))
            {
                Writeback = ExtractInt64(span);
            }
            else if (span.StartsWith("AnonPages"))
            {
                AnonymousPages = ExtractInt64(span);
            }
            else if (span.StartsWith("Mapped"))
            {
                Mapped = ExtractInt64(span);
            }
            else if (span.StartsWith("Shmem"))
            {
                SharedMemory = ExtractInt64(span);
            }
            else if (span.StartsWith("KReclaimable"))
            {
                KernelReclaimable = ExtractInt64(span);
            }
            else if (span.StartsWith("Slab"))
            {
                SlabTotal = ExtractInt64(span);
            }
            else if (span.StartsWith("SReclaimable"))
            {
                SlabReclaimable = ExtractInt64(span);
            }
            else if (span.StartsWith("SUnreclaim"))
            {
                SlabUnreclaimable = ExtractInt64(span);
            }
            else if (span.StartsWith("KernelStack"))
            {
                KernelStack = ExtractInt64(span);
            }
            else if (span.StartsWith("PageTables"))
            {
                PageTables = ExtractInt64(span);
            }
            else if (span.StartsWith("CommitLimit"))
            {
                CommitLimit = ExtractInt64(span);
            }
            else if (span.StartsWith("Committed_AS"))
            {
                CommittedAddressSpace = ExtractInt64(span);
            }
            else if (span.StartsWith("HardwareCorrupted"))
            {
                HardwareCorrupted = ExtractInt64(span);
            }
            // ReSharper restore StringLiteralTypo
        }

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static long ExtractInt64(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && Int64.TryParse(span[range[1]], out var result) ? result : 0;
    }
}
