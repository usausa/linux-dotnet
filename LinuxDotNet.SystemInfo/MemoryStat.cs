namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

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
        var range = (Span<Range>)stackalloc Range[3];
        using var reader = new StreamReader("/proc/meminfo");
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 2)
            {
                continue;
            }

            var value = span[range[1]];
            // ReSharper disable StringLiteralTypo
            switch (span[range[0]])
            {
                case "MemTotal:":
                    MemoryTotal = ParseUInt64(value);
                    break;
                case "MemAvailable:":
                    MemoryAvailable = ParseUInt64(value);
                    break;
                case "MemFree:":
                    MemoryFree = ParseUInt64(value);
                    break;
                case "Buffers:":
                    Buffers = ParseUInt64(value);
                    break;
                case "Cached:":
                    Cached = ParseUInt64(value);
                    break;
                case "SwapCached:":
                    SwapCached = ParseUInt64(value);
                    break;
                case "Active(anon):":
                    ActiveAnonymous = ParseUInt64(value);
                    break;
                case "Inactive(anon):":
                    InactiveAnonymous = ParseUInt64(value);
                    break;
                case "Active(file):":
                    ActiveFile = ParseUInt64(value);
                    break;
                case "Inactive(file):":
                    InactiveFile = ParseUInt64(value);
                    break;
                case "Unevictable:":
                    Unevictable = ParseUInt64(value);
                    break;
                case "Mlocked:":
                    MemoryLocked = ParseUInt64(value);
                    break;
                case "SwapTotal:":
                    SwapTotal = ParseUInt64(value);
                    break;
                case "SwapFree:":
                    SwapFree = ParseUInt64(value);
                    break;
                case "Dirty:":
                    Dirty = ParseUInt64(value);
                    break;
                case "Writeback:":
                    Writeback = ParseUInt64(value);
                    break;
                case "AnonPages:":
                    AnonymousPages = ParseUInt64(value);
                    break;
                case "Mapped:":
                    Mapped = ParseUInt64(value);
                    break;
                case "Shmem:":
                    SharedMemory = ParseUInt64(value);
                    break;
                case "KReclaimable:":
                    KernelReclaimable = ParseUInt64(value);
                    break;
                case "Slab:":
                    SlabTotal = ParseUInt64(value);
                    break;
                case "SReclaimable:":
                    SlabReclaimable = ParseUInt64(value);
                    break;
                case "SUnreclaim:":
                    SlabUnreclaimable = ParseUInt64(value);
                    break;
                case "KernelStack:":
                    KernelStack = ParseUInt64(value);
                    break;
                case "PageTables:":
                    PageTables = ParseUInt64(value);
                    break;
                case "CommitLimit:":
                    CommitLimit = ParseUInt64(value);
                    break;
                case "Committed_AS:":
                    CommittedAddressSpace = ParseUInt64(value);
                    break;
                case "HardwareCorrupted:":
                    HardwareCorrupted = ParseUInt64(value);
                    break;
            }
            // ReSharper restore StringLiteralTypo
        }

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static ulong ParseUInt64(ReadOnlySpan<char> span) =>
        UInt64.TryParse(span, CultureInfo.InvariantCulture, out var result) ? result : 0;
}
