namespace LinuxDotNet.SystemInfo;

public enum DeviceType
{
    Unknown = 0,
    IdeHdd = 3,
    ScsiDisk = 8,
    ScsiCdrom = 11,
    IdeHdd2 = 22,
    IdeHdd3 = 33,
    IdeHdd4 = 34,
    IdeHdd5 = 56,
    IdeHdd6 = 57,
    IdeCdrom = 65,
    IdeCdrom2 = 66,
    IdeCdrom3 = 67,
    IdeCdrom4 = 68,
    IdeCdrom5 = 69,
    IdeCdrom6 = 70,
    IdeCdrom7 = 71,
    Mmc = 179,
    XenVirtual = 202,
    VirtualBlock = 252,
    DeviceMapper = 253,
    MdRaid = 254,
    Nvme = 259
}

internal static class DeviceTypeExtensions
{
    public static bool IsPhysicalStorageDevice(this DeviceType deviceType) =>
        deviceType switch
        {
            DeviceType.IdeHdd => true,
            DeviceType.ScsiDisk => true,
            DeviceType.IdeHdd2 => true,
            DeviceType.IdeHdd3 => true,
            DeviceType.IdeHdd4 => true,
            DeviceType.IdeHdd5 => true,
            DeviceType.IdeHdd6 => true,
            DeviceType.Mmc => true,
            DeviceType.Nvme => true,
            _ => false
        };
}
