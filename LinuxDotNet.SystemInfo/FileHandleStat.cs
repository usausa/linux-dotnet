namespace LinuxDotNet.SystemInfo;

using System;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class FileHandleStat : IDisposable
{
    private readonly KernelFile file;

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public ulong Allocated { get; private set; }

    public ulong Used { get; private set; }

    public ulong Max { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private FileHandleStat(KernelFile file)
    {
        this.file = file;
    }

    internal static FileHandleStat Create()
    {
        var instance = new FileHandleStat(new KernelFile("/proc/sys/fs/file-nr"));
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

        // Three numbers separated by tabs
        var line = TrimEnd(file.Content);
        Allocated = ParseUInt64(NextToken(ref line));
        Used = ParseUInt64(NextToken(ref line));
        Max = ParseUInt64(NextToken(ref line));

        UpdateAt = DateTime.Now;

        return true;
    }
}
