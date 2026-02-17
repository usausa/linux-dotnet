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
    // CPU
    //--------------------------------------------------------------------------------

    public static CpuDevice GetCpu() => new();

    //--------------------------------------------------------------------------------
    // Memory
    //--------------------------------------------------------------------------------

    public static MemoryStat GetMemory() => new();

    public static VirtualMemoryStat GetVirtualMemory() => new();

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<Partition> GetPartitions() => Partition.GetPartitions();

    public static DiskStaticsInfo GetDiskStatics() => new();

    // TODO order ?
    //public static IReadOnlyList<FileSystemEntry> GetFileSystems(bool includeVirtual = false) => FileSystemInfo.GetFileSystems(includeVirtual);

    public static FileDescriptorInfo GetFileDescriptor() => new();

    //--------------------------------------------------------------------------------
    // Network
    //--------------------------------------------------------------------------------

    public static NetworkStaticInfo GetNetworkStatic() => new();

    // TODO
    //public static IReadOnlyList<NetworkInterface> GetNetworkInterfaces() => NetworkInfo.GetInterfaces();

    // TODO
    //public static NetworkInterface? GetNetworkInterface(string name) => NetworkInfo.GetInterface(name);

    public static TcpInfo GetTcp() => new();

    public static TcpInfo GetTcp6() => new(6);

    //--------------------------------------------------------------------------------
    // Process
    //--------------------------------------------------------------------------------

    public static ProcessSummaryInfo GetProcessSummary() => new();

    // TODO
    //public static ProcessEntry? GetProcess(int pid) => ProcessInfo.GetProcess(pid);

    // TODO
    //public static IReadOnlyList<ProcessEntry> GetProcesses() => ProcessInfo.GetProcesses();

    //--------------------------------------------------------------------------------
    // Power supply
    //--------------------------------------------------------------------------------

    public static MainsAdapterDevice GetMainsAdapter() => new();

    public static BatteryDevice GetBattery() => new();

    // Hardware monitor

    public static IReadOnlyList<HardwareMonitor> GetHardwareMonitors() => HardwareMonitor.GetMonitors();
}
