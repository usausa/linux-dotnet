namespace LinuxDotNet.SystemInfo;

#pragma warning disable CA1024
public static class PlatformProvider
{
    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    public static HardwareInfo GetHardware() => new();

    public static KernelInfo GetKernel() => new();

    public static Uptime GetUptime() => new();

    //--------------------------------------------------------------------------------
    // Load
    //--------------------------------------------------------------------------------

    public static SystemStat GetSystemStat() => new();

    public static LoadAverage GetLoadAverage() => new();

    //--------------------------------------------------------------------------------
    // Memory
    //--------------------------------------------------------------------------------

    public static MemoryStat GetMemoryStat() => new();

    public static VirtualMemoryStat GetVirtualMemoryStat() => new();

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    public static DiskStat GetDiskStat() => new();

    public static IReadOnlyList<PartitionInfo> GetPartitions(bool includeAll = false) => PartitionInfo.GetPartitions(includeAll);

    public static IReadOnlyList<MountInfo> GetMounts(bool includeVirtual = false) => MountInfo.GetMounts(includeVirtual);

    //--------------------------------------------------------------------------------
    // Network
    //--------------------------------------------------------------------------------

    public static NetworkStat GetNetworkStat() => new();

    public static TcpStat GetTcpStat() => new();

    public static TcpStat GetTcp6Stat() => new(6);

    //--------------------------------------------------------------------------------
    // Process
    //--------------------------------------------------------------------------------

    public static ProcessSummary GetProcessSummary() => new();

    public static IReadOnlyList<ProcessInfo> GetProcesses() => ProcessInfo.GetProcesses();

    public static ProcessInfo? GetProcess(int pid) => ProcessInfo.GetProcess(pid);

    //--------------------------------------------------------------------------------
    // File
    //--------------------------------------------------------------------------------

    public static FileHandleStat GetFileHandleStat() => new();

    //--------------------------------------------------------------------------------
    // CPU
    //--------------------------------------------------------------------------------

    public static CpuDevice GetCpuDevice() => new();

    //--------------------------------------------------------------------------------
    // Power
    //--------------------------------------------------------------------------------

    public static MainsDevice GetMainsDevice() => new();

    public static BatteryDevice GetBatteryDevice() => new();

    //--------------------------------------------------------------------------------
    // Sensor
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<HardwareMonitor> GetHardwareMonitors() => HardwareMonitor.GetMonitors();
}
