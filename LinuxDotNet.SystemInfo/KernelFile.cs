namespace LinuxDotNet.SystemInfo;

using Microsoft.Win32.SafeHandles;

// A procfs/sysfs file whose handle is kept open and read again from offset 0 (pread) into a reused buffer.
// The first Read (called by the factory at creation) decides whether the file is used:
// - A file that cannot be opened by the first Read is never opened again: every later Read returns false at once, without any
//   syscall or exception. To see a file that appears later, the consumer creates the object again. A factory that creates a
//   list does not add an item whose file could not be opened (Opened is false).
// - Once the file has been opened, a failed read of the held handle closes it and opens it again once (D6), and while the file
//   cannot be opened (it is gone), every Read tries to open it again (hot plug, such as a USB-C power supply or a reloaded
//   hwmon driver).
internal sealed class KernelFile : IDisposable
{
    private readonly bool singleRead;

    private SafeFileHandle? handle;

    private byte[] buffer;

    private int length;

    // The first open failed, so the file is never opened again
    private bool unavailable;

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

    // True once the file has been opened, even when its read fails (the file of a CPU that is offline can be opened, but its
    // read fails with EBUSY until the CPU comes online). A factory that creates a list checks this after creating an item (the
    // first Read) to decide whether to add it
    public bool Opened { get; private set; }

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

    // Reads the file again from the start. False at once when the first open failed. When the held handle fails, it is reopened
    // once and read again (D6)
    public bool Read()
    {
        if (handle is null)
        {
            return !unavailable && Open() && ReadCore();
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
            Opened = true;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Only the first open decides that the file is unavailable. A file opened before is tried again on the next Read
            unavailable = !Opened;
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
