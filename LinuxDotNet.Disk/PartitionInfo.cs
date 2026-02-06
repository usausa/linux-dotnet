namespace LinuxDotNet.Disk;

public sealed class PartitionInfo
{
    public uint Index { get; internal set; }

    public string DeviceName { get; internal set; } = default!;

    public string Name { get; internal set; } = default!;

    public ulong Size { get; internal set; }

    public string? MountPoint { get; internal set; }

    public string? FileSystem { get; internal set; }
}
