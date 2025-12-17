namespace LinuxDotNet.SystemInfo;

using System.Runtime.Versioning;

#pragma warning disable CA1024
[SupportedOSPlatform("linux")]
public static class PlatformProvider
{
    public static UptimeInfo GetUptime() => new();

    public static StaticsInfo GetStatics() => new();

    public static LoadAverageInfo GetLoadAverage() => new();

    public static MemoryInfo GetMemory() => new();

    public static VirtualMemoryInfo GetVirtualMemory() => new();

    public static Partition[] GetPartitions() => Partition.GetPartitions();

    public static DiskStaticsInfo GetDiskStatics() => new();

    public static FileDescriptorInfo GetFileDescriptor() => new();

    public static NetworkStaticInfo GetNetworkStatic() => new();

    public static TcpInfo GetTcp() => new();

    public static TcpInfo GetTcp6() => new(6);

    public static ProcessSummaryInfo GetProcessSummary() => new();

    public static CpuDevice GetCpu() => new();

    public static BatteryDevice GetBattery() => new();

    public static MainsAdapterDevice GetMainsAdapter() => new();

    public static HardwareMonitor[] GetHardwareMonitors() => HardwareMonitor.GetMonitors();
}
