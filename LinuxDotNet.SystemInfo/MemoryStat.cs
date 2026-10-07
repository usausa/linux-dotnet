namespace LinuxDotNet.SystemInfo;

using System;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class MemoryStat : IDisposable
{
    private readonly KernelFile file;

    private bool disposed;

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

    private MemoryStat(KernelFile file)
    {
        this.file = file;
    }

    internal static MemoryStat Create()
    {
        var instance = new MemoryStat(new KernelFile("/proc/meminfo"));
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

            // ReSharper disable StringLiteralTypo
            if (key.SequenceEqual("MemTotal:"u8))
            {
                MemoryTotal = ParseUInt64(value);
            }
            else if (key.SequenceEqual("MemAvailable:"u8))
            {
                MemoryAvailable = ParseUInt64(value);
            }
            else if (key.SequenceEqual("MemFree:"u8))
            {
                MemoryFree = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Buffers:"u8))
            {
                Buffers = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Cached:"u8))
            {
                Cached = ParseUInt64(value);
            }
            else if (key.SequenceEqual("SwapCached:"u8))
            {
                SwapCached = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Active(anon):"u8))
            {
                ActiveAnonymous = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Inactive(anon):"u8))
            {
                InactiveAnonymous = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Active(file):"u8))
            {
                ActiveFile = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Inactive(file):"u8))
            {
                InactiveFile = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Unevictable:"u8))
            {
                Unevictable = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Mlocked:"u8))
            {
                MemoryLocked = ParseUInt64(value);
            }
            else if (key.SequenceEqual("SwapTotal:"u8))
            {
                SwapTotal = ParseUInt64(value);
            }
            else if (key.SequenceEqual("SwapFree:"u8))
            {
                SwapFree = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Dirty:"u8))
            {
                Dirty = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Writeback:"u8))
            {
                Writeback = ParseUInt64(value);
            }
            else if (key.SequenceEqual("AnonPages:"u8))
            {
                AnonymousPages = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Mapped:"u8))
            {
                Mapped = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Shmem:"u8))
            {
                SharedMemory = ParseUInt64(value);
            }
            else if (key.SequenceEqual("KReclaimable:"u8))
            {
                KernelReclaimable = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Slab:"u8))
            {
                SlabTotal = ParseUInt64(value);
            }
            else if (key.SequenceEqual("SReclaimable:"u8))
            {
                SlabReclaimable = ParseUInt64(value);
            }
            else if (key.SequenceEqual("SUnreclaim:"u8))
            {
                SlabUnreclaimable = ParseUInt64(value);
            }
            else if (key.SequenceEqual("KernelStack:"u8))
            {
                KernelStack = ParseUInt64(value);
            }
            else if (key.SequenceEqual("PageTables:"u8))
            {
                PageTables = ParseUInt64(value);
            }
            else if (key.SequenceEqual("CommitLimit:"u8))
            {
                CommitLimit = ParseUInt64(value);
            }
            else if (key.SequenceEqual("Committed_AS:"u8))
            {
                CommittedAddressSpace = ParseUInt64(value);
            }
            else if (key.SequenceEqual("HardwareCorrupted:"u8))
            {
                HardwareCorrupted = ParseUInt64(value);
            }
            // ReSharper restore StringLiteralTypo
        }

        UpdateAt = DateTime.Now;

        return true;
    }
}
