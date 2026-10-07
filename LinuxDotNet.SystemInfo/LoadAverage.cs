namespace LinuxDotNet.SystemInfo;

using System;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class LoadAverage : IDisposable
{
    private readonly KernelFile file;

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public double Average1 { get; private set; }

    public double Average5 { get; private set; }

    public double Average15 { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private LoadAverage(KernelFile file)
    {
        this.file = file;
    }

    // ReSharper disable StringLiteralTypo
    internal static LoadAverage Create()
    {
        var instance = new LoadAverage(new KernelFile("/proc/loadavg"));
        instance.Update();
        return instance;
    }
    // ReSharper restore StringLiteralTypo

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
        Average1 = ParseDouble(NextToken(ref line));
        Average5 = ParseDouble(NextToken(ref line));
        Average15 = ParseDouble(NextToken(ref line));

        UpdateAt = DateTime.Now;

        return true;
    }
}
