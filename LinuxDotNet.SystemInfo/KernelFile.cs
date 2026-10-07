namespace LinuxDotNet.SystemInfo;

using Microsoft.Win32.SafeHandles;

// A procfs/sysfs file whose handle is kept open and read again from offset 0 (pread) into a reused buffer
internal sealed class KernelFile : IDisposable
{
    private readonly bool singleRead;

    private SafeFileHandle? handle;

    private byte[] buffer;

    private int length;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KernelFile(string path, int bufferSize = 4096, bool singleRead = false)
    {
        Path = path;
        this.singleRead = singleRead;
        buffer = new byte[bufferSize];
    }

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    public string Path { get; }

    public bool IsOpen => handle is not null;

    public ReadOnlySpan<byte> Content => buffer.AsSpan(0, length);

    //--------------------------------------------------------------------------------
    // Close
    //--------------------------------------------------------------------------------

    public void Dispose() => Close();

    public void Close()
    {
        handle?.Dispose();
        handle = null;
        length = 0;
    }

    //--------------------------------------------------------------------------------
    // Read
    //--------------------------------------------------------------------------------

    // Reads the file again from the start. When the held handle fails, it is reopened once and read again (D6)
    public bool Read()
    {
        if (handle is null)
        {
            return Open() && ReadCore();
        }

        if (ReadCore())
        {
            return true;
        }

        Close();
        return Open() && ReadCore();
    }

    private bool Open()
    {
        try
        {
            handle = File.OpenHandle(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private bool ReadCore()
    {
        try
        {
            var total = 0;
            while (true)
            {
                if (total == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                var read = RandomAccess.Read(handle!, buffer.AsSpan(total), total);
                if (read == 0)
                {
                    break;
                }

                total += read;

                // A single value file of sysfs returns the whole content by one read, so the pread to confirm EOF is omitted
                if (singleRead && (total < buffer.Length))
                {
                    break;
                }
            }

            length = total;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            length = 0;
            return false;
        }
    }
}
