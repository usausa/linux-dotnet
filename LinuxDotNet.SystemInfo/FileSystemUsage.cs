namespace LinuxDotNet.SystemInfo;

using static LinuxDotNet.SystemInfo.NativeMethods;

public sealed class FileSystemUsage
{
    public string Path { get; }

    public DateTime UpdateAt { get; private set; }

    public ulong TotalSize { get; private set; }

    public ulong FreeSize { get; private set; }

    public ulong AvailableSize { get; private set; }

    public ulong BlockSize { get; private set; }

    public ulong TotalFiles { get; private set; }

    public ulong FreeFiles { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal FileSystemUsage(string path)
    {
        Path = path;
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        var buf = default(statfs);
        if (statfs64(Path, ref buf) != 0)
        {
            return false;
        }

        var blockSize = buf.f_frsize != 0 ? buf.f_frsize : buf.f_bsize;

        TotalSize = buf.f_blocks * blockSize;
        FreeSize = buf.f_bfree * blockSize;
        AvailableSize = buf.f_bavail * blockSize;
        BlockSize = blockSize;
        TotalFiles = buf.f_files;
        FreeFiles = buf.f_ffree;

        UpdateAt = DateTime.Now;

        return true;
    }
}
