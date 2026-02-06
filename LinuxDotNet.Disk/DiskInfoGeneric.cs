namespace LinuxDotNet.Disk;

internal sealed class DiskInfoGeneric : IDiskInfo
{
    public uint Index { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string SerialNumber { get; set; } = string.Empty;

    public string FirmwareRevision { get; set; } = string.Empty;

    public ulong Size { get; set; }

    public uint PhysicalBlockSize { get; set; }

    public uint LogicalBlockSize { get; set; }

    public ulong TotalSectors { get; set; }

    public bool Removable { get; set; }

    public DiskType DiskType { get; set; }

    public SmartType SmartType { get; set; }

    public ISmart Smart { get; set; } = default!;

    public void Dispose()
    {
        (Smart as IDisposable)?.Dispose();
    }
}
