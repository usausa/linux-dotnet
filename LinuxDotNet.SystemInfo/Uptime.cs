namespace LinuxDotNet.SystemInfo;

using System;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class Uptime : IDisposable
{
    private readonly KernelFile file;

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public TimeSpan Elapsed { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private Uptime(KernelFile file)
    {
        this.file = file;
    }

    internal static Uptime Create()
    {
        var instance = new Uptime(new KernelFile("/proc/uptime"));
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

        var line = TrimEnd(file.Content);
        Elapsed = TimeSpan.FromSeconds(ParseDouble(NextToken(ref line)));

        UpdateAt = DateTime.Now;

        return true;
    }
}
