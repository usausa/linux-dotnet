namespace LinuxDotNet.SystemInfo;

using Microsoft.Win32.SafeHandles;

internal sealed class KernelFile : IDisposable
{
    private readonly bool singleRead;

    private SafeFileHandle? handle;

    private byte[] buffer;

    private int length;

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
