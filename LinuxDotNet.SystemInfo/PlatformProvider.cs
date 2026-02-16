namespace LinuxDotNet.SystemInfo;

#pragma warning disable CA1024
public static class PlatformProvider
{
    // TODO
    //public static HardwareInfo GetHardware() => new();

    public static KernelInfo GetKernel() => new();

    public static UptimeInfo GetUptime() => new();

    public static StaticsInfo GetStatics() => new();

    public static LoadAverageInfo GetLoadAverage() => new();

    public static MemoryInfo GetMemory() => new();

    public static VirtualMemoryInfo GetVirtualMemory() => new();

    public static IReadOnlyList<Partition> GetPartitions() => Partition.GetPartitions();

    public static DiskStaticsInfo GetDiskStatics() => new();

    // TODO order ?
    //public static IReadOnlyList<FileSystemEntry> GetFileSystems(bool includeVirtual = false) => FileSystemInfo.GetFileSystems(includeVirtual);

    public static FileDescriptorInfo GetFileDescriptor() => new();

    public static NetworkStaticInfo GetNetworkStatic() => new();

    // TODO
    //public static IReadOnlyList<NetworkInterface> GetNetworkInterfaces() => NetworkInfo.GetInterfaces();

    // TODO
    //public static NetworkInterface? GetNetworkInterface(string name) => NetworkInfo.GetInterface(name);

    public static TcpInfo GetTcp() => new();

    public static TcpInfo GetTcp6() => new(6);

    public static ProcessSummaryInfo GetProcessSummary() => new();

    // TODO
    //public static ProcessEntry? GetProcess(int pid) => ProcessInfo.GetProcess(pid);

    // TODO
    //public static IReadOnlyList<ProcessEntry> GetProcesses() => ProcessInfo.GetProcesses();

    public static CpuDevice GetCpu() => new();

    public static BatteryDevice GetBattery() => new();

    public static MainsAdapterDevice GetMainsAdapter() => new();

    public static IReadOnlyList<HardwareMonitor> GetHardwareMonitors() => HardwareMonitor.GetMonitors();
}
