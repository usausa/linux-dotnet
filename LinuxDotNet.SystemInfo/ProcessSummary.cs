namespace LinuxDotNet.SystemInfo;

using System.Globalization;
using System.IO.Enumeration;

using Microsoft.Win32.SafeHandles;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class ProcessSummary : IDisposable
{
    private const string ProcPath = "/proc";

    // Same as Directory.EnumerateDirectories (no attribute is skipped)
    private static readonly EnumerationOptions ProcessDirectoryOptions = new() { AttributesToSkip = 0 };

    // Buffer for /proc/<pid>/status, reused for every process and grown when it is full
    private byte[] buffer = new byte[4096];

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public int ProcessCount { get; private set; }

    public int ThreadCount { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private ProcessSummary()
    {
    }

    internal static ProcessSummary Create()
    {
        var instance = new ProcessSummary();
        instance.Update();
        return instance;
    }

    public void Dispose()
    {
        disposed = true;
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var process = 0;
        var thread = 0;
        try
        {
            // The pid directories, with the pid parsed from the name
            var processIds = new FileSystemEnumerable<int>(
                ProcPath,
                static (ref FileSystemEntry entry) => TryParseProcessId(entry.FileName, out var id) ? id : 0,
                ProcessDirectoryOptions)
            {
                ShouldIncludePredicate = static (ref FileSystemEntry entry) => TryParseProcessId(entry.FileName, out _) && entry.IsDirectory
            };

            foreach (var pid in processIds)
            {
                process++;
                thread += ReadThreadCount(pid);
            }
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
    private static bool TryParseProcessId(ReadOnlySpan<char> name, out int pid) =>
        Int32.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out pid);

    // Threads in /proc/<pid>/status (0 when the process has exited)
    private int ReadThreadCount(int pid)
    {
        var path = String.Create(CultureInfo.InvariantCulture, $"/proc/{pid}/status");
        int length;
        try
        {
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            length = ReadAll(handle);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }

        var remaining = new ReadOnlySpan<byte>(buffer, 0, length);
        while (TryReadLine(ref remaining, out var line))
        {
            if (line.StartsWith("Threads:"u8))
            {
                var value = line["Threads:"u8.Length..];
                return ParseInt32(NextToken(ref value));
            }
        }

        return 0;
    }

    // Reads the whole file from the start into the buffer (grown when it is full) and returns the length
    private int ReadAll(SafeFileHandle handle)
    {
        var total = 0;
        while (true)
        {
            if (total == buffer.Length)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            var read = RandomAccess.Read(handle, buffer.AsSpan(total), total);
            if (read == 0)
            {
                return total;
            }

            total += read;
        }
    }
}
