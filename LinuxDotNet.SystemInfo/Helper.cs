namespace LinuxDotNet.SystemInfo;

internal static class Helper
{
    public static bool IsTargetDriveType(int major) =>
        major switch
        {
            3 => true, // HDD
            8 => true, // IDE
            22 => true,
            179 => true, // MMC
            239 => true, // NVMe
            _ => false
        };
}
