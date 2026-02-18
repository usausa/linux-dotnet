namespace LinuxDotNet.SystemInfo;

public sealed class FileSystemUsage
{
    public ulong TotalSize { get; }

    public ulong FreeSize { get; }

    public ulong AvailableSize { get; }

    public ulong BlockSize { get; }

    public ulong TotalFiles { get; }

    public ulong FreeFiles { get; }

    internal FileSystemUsage(ulong totalSize, ulong freeSize, ulong availableSize, ulong blockSize, ulong totalFiles, ulong freeFiles)
    {
        TotalSize = totalSize;
        FreeSize = freeSize;
        AvailableSize = availableSize;
        BlockSize = blockSize;
        TotalFiles = totalFiles;
        FreeFiles = freeFiles;
    }
}
