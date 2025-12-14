namespace Example.Video4Linux2.AvaloniaApp.Helper;

public sealed class BufferManager : IDisposable
{
    public sealed class BufferSlot
    {
        private readonly BufferManager manager;

        private readonly int slotNo;

        private readonly byte[] buffer;

        private readonly int bufferSize;

        public Span<byte> Buffer => buffer.AsSpan(0, bufferSize);

        public Lock Lock { get; }

        public int SlotNo => slotNo;

        public int Version { get; private set; }

        public DateTime LastUpdated { get; private set; }

        public BufferSlot(BufferManager manager, int slotNo, byte[] buffer, int bufferSize)
        {
            this.manager = manager;
            this.slotNo = slotNo;

            buffer.AsSpan().Clear();
            this.buffer = buffer;
            this.bufferSize = bufferSize;

            Lock = new Lock();
            Version = 0;
            LastUpdated = DateTime.MinValue;
        }

        public void MarkUpdated()
        {
            Version++;
            LastUpdated = DateTime.Now;

            manager.NotifyUpdate(slotNo);
        }
    }

    private readonly BufferSlot[] slots;

    private readonly Lock indexLock = new();

    private int nextIndex;
    private int lastUpdatedIndex = -1;

    private byte[][] buffers;

    private bool disposed;

    public int SlotCount => slots.Length;

    public BufferManager(int slotCount, int bufferSize)
    {
        slots = new BufferSlot[slotCount];
        buffers = new byte[slotCount][];

        for (var i = 0; i < slotCount; i++)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            buffers[i] = buffer;
            slots[i] = new BufferSlot(this, i, buffer, bufferSize);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (var buffer in buffers)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        buffers = [];

        disposed = true;
    }

    public BufferSlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return slots[index];
    }

    public BufferSlot NextSlot()
    {
        lock (indexLock)
        {
            var index = nextIndex;
            nextIndex = (nextIndex + 1) % slots.Length;
            return slots[index];
        }
    }

    public BufferSlot? LastUpdateSlot()
    {
        lock (indexLock)
        {
            return lastUpdatedIndex < 0 ? null : slots[lastUpdatedIndex];
        }
    }

    private void NotifyUpdate(int slotNo)
    {
        lock (indexLock)
        {
            lastUpdatedIndex = slotNo;
        }
    }
}
