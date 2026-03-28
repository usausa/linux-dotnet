namespace LinuxDotNet.Disk;

public interface IDiskInfo : IDisposable
{
    uint Index { get; }

    string DeviceName { get; }

    string Model { get; }

    string SerialNumber { get; }

    string FirmwareRevision { get; }

    ulong Size { get; }

    uint PhysicalBlockSize { get; }

    uint LogicalBlockSize { get; }

    ulong TotalSectors { get; }

    bool Removable { get; }

    DiskType DiskType { get; }

    SmartType SmartType { get; }

    ISmart Smart { get; }
}
