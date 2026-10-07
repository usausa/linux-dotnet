namespace LinuxDotNet.SystemInfo;

using System.Globalization;
using System.IO.Enumeration;

using static LinuxDotNet.SystemInfo.KernelFileParser;

// ProcessCount is the number of processes visible in /proc (the pid directories, counted without opening anything per process).
// ThreadCount is the number of threads (tasks, including kernel threads) that currently exist on the whole system, taken from
// /proc/loadavg. In a container with its own pid namespace, or with the hidepid mount option, ProcessCount counts only the
// visible processes while ThreadCount is for the whole system.
public sealed class ProcessSummary : IDisposable
{
    private const string ProcPath = "/proc";

    // Same as Directory.EnumerateDirectories (no attribute is skipped)
    private static readonly EnumerationOptions ProcessDirectoryOptions = new() { AttributesToSkip = 0 };

    private readonly KernelFile file;

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public int ProcessCount { get; private set; }

    public int ThreadCount { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private ProcessSummary(KernelFile file)
    {
        this.file = file;
    }

    internal static ProcessSummary Create()
    {
        var instance = new ProcessSummary(new KernelFile("/proc/loadavg"));
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

        // The 4th field is running/total (e.g. 0.22 0.08 0.19 1/382 964112), and the total is the number of threads
        var line = TrimEnd(file.Content);
        _ = NextToken(ref line);
        _ = NextToken(ref line);
        _ = NextToken(ref line);
        var tasks = NextToken(ref line);
        var separator = tasks.IndexOf((byte)'/');
        var thread = (separator >= 0) ? ParseInt32(tasks[(separator + 1)..]) : 0;

        int process;
        try
        {
            // The pid directories are only counted (nothing is opened for each process)
            var processes = new FileSystemEnumerable<bool>(
                ProcPath,
                static (ref FileSystemEntry entry) => true,
                ProcessDirectoryOptions)
            {
                ShouldIncludePredicate = static (ref FileSystemEntry entry) => IsProcessId(entry.FileName) && entry.IsDirectory
            };

            process = processes.Count();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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

    // All digits
    private static bool IsProcessId(ReadOnlySpan<char> name) =>
        Int32.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out _);
}
