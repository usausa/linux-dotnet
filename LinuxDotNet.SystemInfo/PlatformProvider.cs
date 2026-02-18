namespace LinuxDotNet.SystemInfo;

#pragma warning disable CA1024
public static class PlatformProvider
{
    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    // TODO
    //public static HardwareInfo GetHardware() => new();

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

    public static IReadOnlyList<Partition> GetPartitions() => Partition.GetPartitions();

    public static DiskStat GetDiskStat() => new();

    // TODO order ?
    //public static IReadOnlyList<FileSystemEntry> GetFileSystems(bool includeVirtual = false) => FileSystemInfo.GetFileSystems(includeVirtual);

    public static FileHandleStat GetFileHandleStat() => new();

    //--------------------------------------------------------------------------------
    // Network
    //--------------------------------------------------------------------------------

    public static NetworkStat GetNetworkStat() => new();

    // TODO statはstat側？
    //public static IReadOnlyList<NetworkInterface> GetNetworkInterfaces() => NetworkInfo.GetInterfaces();

    // TODO
    //public static NetworkInterface? GetNetworkInterface(string name) => NetworkInfo.GetInterface(name);

    public static TcpStat GetTcpStat() => new();

    public static TcpStat GetTcp6Stat() => new(6);

    //--------------------------------------------------------------------------------
    // Process
    //--------------------------------------------------------------------------------

    public static ProcessSummary GetProcessSummary() => new();

    public static IReadOnlyList<ProcessInfo> GetProcesses() => ProcessInfo.GetProcesses();

    public static ProcessInfo? GetProcess(int pid) => ProcessInfo.GetProcess(pid);

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
