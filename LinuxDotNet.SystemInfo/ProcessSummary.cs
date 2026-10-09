namespace LinuxDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static LinuxDotNet.SystemInfo.KernelFileParser;
using static LinuxDotNet.SystemInfo.NativeMethods;

public sealed class ProcessSummary : IDisposable
{
    private readonly KernelFile file;

    private readonly SafeDirectoryHandle directory;

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public int ProcessCount { get; private set; }

    public int ThreadCount { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private ProcessSummary(KernelFile file, SafeDirectoryHandle directory)
    {
        this.file = file;
        this.directory = directory;
    }

    internal static ProcessSummary Create()
    {
        var instance = new ProcessSummary(new KernelFile("/proc/loadavg"), new SafeDirectoryHandle(opendir("/proc")));
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
        directory.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!file.Read() || directory.IsInvalid)
        {
            return false;
        }

        // 4 field is running/total (e.g. 0.22 0.08 0.19 1/382 964112)
        var line = TrimEnd(file.Content);
        _ = NextToken(ref line);
        _ = NextToken(ref line);
        _ = NextToken(ref line);
        var tasks = NextToken(ref line);
        var separator = tasks.IndexOf((byte)'/');
        var thread = (separator >= 0) ? ParseInt32(tasks[(separator + 1)..]) : 0;

        var process = CountProcesses();
        if (process < 0)
        {
            return false;
        }

        ProcessCount = process;
        ThreadCount = thread;

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private unsafe int CountProcesses()
    {
        rewinddir(directory);

        var count = 0;
        while (true)
        {
            var entry = readdir64(directory);
            if (entry is null)
            {
                return Marshal.GetLastPInvokeError() == 0 ? count : -1;
            }

            if ((entry->d_type == DT_DIR) && IsProcessId(entry->d_name))
            {
                count++;
            }
        }
    }

    private static unsafe bool IsProcessId(byte* name)
    {
        if (*name == 0)
        {
            return false;
        }

        for (; *name != 0; name++)
        {
            if ((uint)(*name - '0') > 9)
            {
                return false;
            }
        }

        return true;
    }
}
