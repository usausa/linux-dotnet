namespace LinuxDotNet.SystemInfo;

#pragma warning disable CA1024
public static class PlatformProvider
{
    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    public static HardwareInfo GetHardware() => HardwareInfo.Create();

    public static KernelInfo GetKernel() => KernelInfo.Create();

    public static Uptime GetUptime() => Uptime.Create();

    //--------------------------------------------------------------------------------
    // Load
    //--------------------------------------------------------------------------------

    public static SystemStat GetSystemStat() => SystemStat.Create();

    public static LoadAverage GetLoadAverage() => LoadAverage.Create();

    //--------------------------------------------------------------------------------
    // Memory
    //--------------------------------------------------------------------------------

    public static MemoryStat GetMemoryStat() => MemoryStat.Create();

    public static VirtualMemoryStat GetVirtualMemoryStat() => VirtualMemoryStat.Create();

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    public static DiskStat GetDiskStat() => DiskStat.Create();

    public static IReadOnlyList<PartitionInfo> GetPartitions(bool includeAll = false) => PartitionInfo.GetPartitions(includeAll);

    public static IReadOnlyList<MountInfo> GetMounts(bool includeVirtual = false) => MountInfo.GetMounts(includeVirtual);

    public static FileSystemUsage GetFileSystemUsage(string path) => FileSystemUsage.Create(path);

    //--------------------------------------------------------------------------------
    // Network
    //--------------------------------------------------------------------------------

    public static NetworkStat GetNetworkStat() => NetworkStat.Create();

    public static TcpStat GetTcpStat() => TcpStat.Create(null);

    public static TcpStat GetTcp6Stat() => TcpStat.Create(6);

    public static WirelessStat GetWirelessStat() => WirelessStat.Create();

    //--------------------------------------------------------------------------------
    // Process
    //--------------------------------------------------------------------------------

    public static ProcessSummary GetProcessSummary() => ProcessSummary.Create();

    public static IReadOnlyList<ProcessInfo> GetProcesses() => ProcessInfo.GetProcesses();

    public static ProcessInfo? GetProcess(int processId) => ProcessInfo.GetProcess(processId);

    //--------------------------------------------------------------------------------
    // File
    //--------------------------------------------------------------------------------

    public static FileHandleStat GetFileHandleStat() => FileHandleStat.Create();

    //--------------------------------------------------------------------------------
    // CPU
    //--------------------------------------------------------------------------------

    public static CpuDevice GetCpuDevice() => CpuDevice.Create();

    //--------------------------------------------------------------------------------
    // Power
    //--------------------------------------------------------------------------------

    public static MainsDevice GetMainsDevice() => MainsDevice.Create();

    public static BatteryDevice GetBatteryDevice() => BatteryDevice.Create();

    //--------------------------------------------------------------------------------
    // Sensor
    //--------------------------------------------------------------------------------

    // Each element must be disposed
    public static IReadOnlyList<HardwareMonitor> GetHardwareMonitors() => HardwareMonitor.GetMonitors();

    //--------------------------------------------------------------------------------
    // USB
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<UsbDevice> GetUsbDevices(bool includeRootHub = false) => UsbDevice.GetDevices(includeRootHub);
}
