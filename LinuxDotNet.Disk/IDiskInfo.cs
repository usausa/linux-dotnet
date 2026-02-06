namespace LinuxDotNet.Disk;

public interface IDiskInfo : IDisposable
{
    public uint Index { get; }

    public string DeviceName { get; }

    public string Model { get; }

    public string SerialNumber { get; }

    public string FirmwareRevision { get; }

    public ulong Size { get; }

    public uint PhysicalBlockSize { get; }

    public uint LogicalBlockSize { get; }

    public ulong TotalSectors { get; }

    public bool Removable { get; }

    public DiskType DiskType { get; }

    public SmartType SmartType { get; }

    public ISmart Smart { get; }
}
