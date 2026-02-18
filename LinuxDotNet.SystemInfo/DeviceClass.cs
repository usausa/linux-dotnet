namespace LinuxDotNet.SystemInfo;

public enum DeviceClass
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

internal static class DeviceClassExtensions
{
    public static bool IsPhysicalStorage(this DeviceClass deviceClass) =>
        deviceClass switch
        {
            DeviceClass.IdeHdd => true,
            DeviceClass.ScsiDisk => true,
            DeviceClass.IdeHdd2 => true,
            DeviceClass.IdeHdd3 => true,
            DeviceClass.IdeHdd4 => true,
            DeviceClass.IdeHdd5 => true,
            DeviceClass.IdeHdd6 => true,
            DeviceClass.Mmc => true,
            DeviceClass.Nvme => true,
            _ => false
        };
}
