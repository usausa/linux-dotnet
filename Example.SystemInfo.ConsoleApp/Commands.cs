// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Example.SystemInfo.ConsoleApp;

using LinuxDotNet.SystemInfo;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<HardwareCommand>();
        commands.AddCommand<KernelCommand>();
        commands.AddCommand<UptimeCommand>();
        commands.AddCommand<StatCommand>();
        commands.AddCommand<LoadCommand>();
        commands.AddCommand<MemoryCommand>();
        commands.AddCommand<VirtualCommand>();
        commands.AddCommand<PartitionCommand>();
        commands.AddCommand<MountCommand>();
        commands.AddCommand<DiskCommand>();
        commands.AddCommand<NetworkCommand>();
        commands.AddCommand<TcpCommand>();
        commands.AddCommand<Tcp6Command>();
        commands.AddCommand<WirelessCommand>();
        commands.AddCommand<ProcessCommand>();
        commands.AddCommand<ProcessesCommand>();
        commands.AddCommand<FdCommand>();
        commands.AddCommand<CpuCommand>();
        commands.AddCommand<AcCommand>();
        commands.AddCommand<BatteryCommand>();
        commands.AddCommand<HwmonCommand>();
    }
}

//--------------------------------------------------------------------------------
// Hardware
//--------------------------------------------------------------------------------
[Command("hardware", "Get hardware information")]
public sealed class HardwareCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var hw = PlatformProvider.GetHardware();

        Console.WriteLine("[DMI]");
        Console.WriteLine($"Vendor:         {hw.Vendor}");
        Console.WriteLine($"ProductName:    {hw.ProductName}");
        Console.WriteLine($"ProductVersion: {hw.ProductVersion}");
        Console.WriteLine($"SerialNumber:   {hw.SerialNumber}");

        Console.WriteLine("[BIOS]");
        Console.WriteLine($"Vendor:         {hw.BiosVendor}");
        Console.WriteLine($"Version:        {hw.BiosVersion}");
        Console.WriteLine($"Date:           {hw.BiosDate}");
        Console.WriteLine($"Release:        {hw.BiosRelease}");

        Console.WriteLine("[Board]");
        Console.WriteLine($"Vendor:         {hw.BoardVendor}");
        Console.WriteLine($"Name:           {hw.BoardName}");
        Console.WriteLine($"Version:        {hw.BoardVersion}");
        Console.WriteLine("[CPU]");
        Console.WriteLine($"Brand:          {hw.CpuBrandString}");
        Console.WriteLine($"Vendor:         {hw.CpuVendor}");
        Console.WriteLine($"Family:         {hw.CpuFamily}");
        Console.WriteLine($"Model:          {hw.CpuModel}");
        Console.WriteLine($"Stepping:       {hw.CpuStepping}");
        Console.WriteLine($"Logical:        {hw.LogicalCpu}");
        Console.WriteLine($"Physical:       {hw.PhysicalCpu}");
        Console.WriteLine($"CoresPerSocket: {hw.CoresPerSocket}");
        Console.WriteLine($"FrequencyMax:   {hw.CpuFrequencyMax / 1_000_000.0:F0} MHz");

        Console.WriteLine("[Cache]");
        Console.WriteLine($"L1D:            {hw.L1DCacheSize / 1024} KB");
        Console.WriteLine($"L1I:            {hw.L1ICacheSize / 1024} KB");
        Console.WriteLine($"L2:             {hw.L2CacheSize / 1024} KB");
        Console.WriteLine($"L3:             {hw.L3CacheSize / 1024} KB");

        Console.WriteLine("[Memory]");
        Console.WriteLine($"MemoryTotal:    {hw.MemoryTotal / 1024 / 1024} MB");
        Console.WriteLine($"PageSize:       {hw.PageSize} bytes");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Kernel
//--------------------------------------------------------------------------------
[Command("kernel", "Get kernel information")]
public sealed class KernelCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var kernel = PlatformProvider.GetKernel();

        Console.WriteLine($"OsType:                 {kernel.OsType}");
        Console.WriteLine($"OsRelease:              {kernel.OsRelease}");
        Console.WriteLine($"KernelVersion:          {kernel.KernelVersion}");
        Console.WriteLine($"OsProductVersion:       {kernel.OsProductVersion}");
        Console.WriteLine($"OsName:                 {kernel.OsName}");
        Console.WriteLine($"OsPrettyName:           {kernel.OsPrettyName}");
        Console.WriteLine($"OsId:                   {kernel.OsId}");
        Console.WriteLine($"BootTime:               {kernel.BootTime}");
        Console.WriteLine($"MaxProcessCount:        {kernel.MaxProcessCount}");
        Console.WriteLine($"MaxFileCount:           {kernel.MaxFileCount}");
        Console.WriteLine($"MaxFileCountPerProcess: {kernel.MaxFileCountPerProcess}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Uptime
//--------------------------------------------------------------------------------
[Command("uptime", "Get uptime")]
public sealed class UptimeCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var uptime = PlatformProvider.GetUptime();

        Console.WriteLine($"Elapsed: {uptime.Elapsed}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Stat
//--------------------------------------------------------------------------------
[Command("stat", "Get system stat")]
public sealed class StatCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var stat = PlatformProvider.GetSystemStat();

        Console.WriteLine($"Interrupt:      {stat.Interrupt}");
        Console.WriteLine($"ContextSwitch:  {stat.ContextSwitch}");
        Console.WriteLine($"SoftIrq:        {stat.SoftIrq}");
        Console.WriteLine($"Forks:          {stat.Forks}");
        Console.WriteLine($"RunnableTasks:  {stat.RunnableTasks}");
        Console.WriteLine($"BlockedTasks:   {stat.BlockedTasks}");

        Console.WriteLine($"User:           {stat.CpuTotal.User}");
        Console.WriteLine($"Nice:           {stat.CpuTotal.Nice}");
        Console.WriteLine($"System:         {stat.CpuTotal.System}");
        Console.WriteLine($"Idle:           {stat.CpuTotal.Idle}");
        Console.WriteLine($"IoWait:         {stat.CpuTotal.IoWait}");
        Console.WriteLine($"Irq:            {stat.CpuTotal.Irq}");
        Console.WriteLine($"SoftIrq:        {stat.CpuTotal.SoftIrq}");
        Console.WriteLine($"Steal:          {stat.CpuTotal.Steal}");
        Console.WriteLine($"Guest:          {stat.CpuTotal.Guest}");
        Console.WriteLine($"GuestNice:      {stat.CpuTotal.GuestNice}");

        Console.WriteLine();

        for (var i = 0; i < 10; i++)
        {
            var previousValues = stat.CpuCores
                .Select(x =>
                {
                    var nonIdle = CalcCpuNonIdle(x);
                    var total = nonIdle + CalcCpuIdle(x);
                    return new
                    {
                        NonIdle = nonIdle,
                        Total = total
                    };
                })
                .ToList();

            await Task.Delay(1000);

            stat.Update();

            for (var j = 0; j < stat.CpuCores.Count; j++)
            {
                var core = stat.CpuCores[j];
                var nonIdle = CalcCpuNonIdle(core);
                var total = nonIdle + CalcCpuIdle(core);

                var nonIdleDiff = nonIdle - previousValues[j].NonIdle;
                var totalDiff = total - previousValues[j].Total;
                var usage = totalDiff > 0 ? (int)Math.Ceiling((double)nonIdleDiff / totalDiff * 100.0) : 0;

                Console.WriteLine($"Name:  {core.Name}");
                Console.WriteLine($"Usage: {usage}");
            }
        }

        static long CalcCpuIdle(CpuStat cpu)
        {
            return cpu.Idle + cpu.IoWait;
        }

        static long CalcCpuNonIdle(CpuStat cpu)
        {
            return cpu.User + cpu.Nice + cpu.System + cpu.Irq + cpu.SoftIrq + cpu.Steal;
        }
    }
}

//--------------------------------------------------------------------------------
// LoadAverage
//--------------------------------------------------------------------------------
[Command("load", "Get load average")]
public sealed class LoadCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var load = PlatformProvider.GetLoadAverage();

        Console.WriteLine($"Average1:  {load.Average1:F2}");
        Console.WriteLine($"Average5:  {load.Average5:F2}");
        Console.WriteLine($"Average15: {load.Average15:F2}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Memory
//--------------------------------------------------------------------------------
[Command("memory", "Get memory stat")]
public sealed class MemoryCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var memory = PlatformProvider.GetMemoryStat();
        var usage = (int)Math.Ceiling((double)(memory.MemoryTotal - memory.MemoryAvailable) / memory.MemoryTotal * 100);

        Console.WriteLine($"MemoryTotal:     {memory.MemoryTotal}");
        Console.WriteLine($"MemoryAvailable: {memory.MemoryAvailable}");
        Console.WriteLine($"Buffers:         {memory.Buffers}");
        Console.WriteLine($"Cached:          {memory.Cached}");
        Console.WriteLine($"Usage:           {usage}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// VirtualMemory
//--------------------------------------------------------------------------------
[Command("virtual", "Get virtual memory stat")]
public sealed class VirtualCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var vm = PlatformProvider.GetVirtualMemoryStat();

        Console.WriteLine($"PageIn:            {vm.PageIn}");
        Console.WriteLine($"PageOut:           {vm.PageOut}");
        Console.WriteLine($"SwapIn:            {vm.SwapIn}");
        Console.WriteLine($"SwapOut:           {vm.SwapOut}");
        Console.WriteLine($"PageFaults:        {vm.PageFaults}");
        Console.WriteLine($"MajorPageFaults:   {vm.MajorPageFaults}");
        Console.WriteLine($"OutOfMemoryKiller: {vm.OutOfMemoryKiller}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Partition
//--------------------------------------------------------------------------------
[Command("partition", "Get partition")]
public sealed class PartitionCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var partitions = PlatformProvider.GetPartitions();
        foreach (var partition in partitions)
        {
            Console.WriteLine($"Name:          {partition.Name}");
            Console.WriteLine($"DeviceClass:   {partition.DeviceClass} ({(int)partition.DeviceClass})");
            Console.WriteLine($"No:            {partition.No}");
            Console.WriteLine($"Blocks:        {partition.Blocks}");

            var mounts = partition.GetMounts();
            if (mounts.Count > 0)
            {
                Console.WriteLine($"MountPoint:    {String.Join(' ', mounts.Select(m => m.MountPoint))}");
            }

            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Mount
//--------------------------------------------------------------------------------
[Command("mount", "Get mount points")]
public sealed class MountCommand : ICommandHandler
{
    [Option("--virtual", "-v", Description = "Include virtual file systems")]
    public bool IncludeVirtual { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var mounts = PlatformProvider.GetMounts(IncludeVirtual);
        foreach (var mount in mounts)
        {
            Console.WriteLine($"Device:        {mount.DeviceName}");
            Console.WriteLine($"MountPoint:    {mount.MountPoint}");
            Console.WriteLine($"FileSystem:    {mount.FileSystem}");
            Console.WriteLine($"Options:       {mount.Option}");
            Console.WriteLine($"IsLocal:       {mount.IsLocal}");

            var usage = PlatformProvider.GetFileSystemUsage(mount.MountPoint);
            var used = usage.TotalSize - usage.FreeSize;
            var usagePercent = (used + usage.AvailableSize) > 0
                ? (int)Math.Ceiling((double)used / (used + usage.AvailableSize) * 100)
                : 0;
            Console.WriteLine($"TotalSize:     {usage.TotalSize}");
            Console.WriteLine($"FreeSize:      {usage.FreeSize}");
            Console.WriteLine($"AvailableSize: {usage.AvailableSize}");
            Console.WriteLine($"Usage:         {usagePercent}%");
            Console.WriteLine($"TotalFiles:    {usage.TotalFiles}");
            Console.WriteLine($"FreeFiles:     {usage.FreeFiles}");

            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// DiskStat
//--------------------------------------------------------------------------------
[Command("disk", "Get disk stat")]
public sealed class DiskCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var disk = PlatformProvider.GetDiskStat();
        foreach (var device in disk.Devices)
        {
            Console.WriteLine($"Name:           {device.Name}");
            Console.WriteLine($"ReadCompleted:  {device.ReadCompleted}");
            Console.WriteLine($"ReadMerged:     {device.ReadMerged}");
            Console.WriteLine($"ReadSectors:    {device.ReadSectors}");
            Console.WriteLine($"ReadTime:       {device.ReadTime}");
            Console.WriteLine($"WriteCompleted: {device.WriteCompleted}");
            Console.WriteLine($"WriteMerged:    {device.WriteMerged}");
            Console.WriteLine($"WriteSectors:   {device.WriteSectors}");
            Console.WriteLine($"WriteTime:      {device.WriteTime}");
            Console.WriteLine($"IosInProgress:  {device.IosInProgress}");
            Console.WriteLine($"IoTime:         {device.IoTime}");
            Console.WriteLine($"WeightIoTime:   {device.WeightIoTime}");
        }

        Console.WriteLine();

        for (var i = 0; i < 10; i++)
        {
            var previousUpdateAt = disk.UpdateAt;
            var previousValues = disk.Devices
                .Select(x => new
                {
                    x.ReadCompleted,
                    x.WriteCompleted
                })
                .ToList();

            await Task.Delay(1000);

            disk.Update();

            var timespan = (disk.UpdateAt - previousUpdateAt).TotalSeconds;
            for (var j = 0; j < disk.Devices.Count; j++)
            {
                var device = disk.Devices[j];
                var readPerSec = (int)Math.Ceiling((device.ReadCompleted - previousValues[j].ReadCompleted) / timespan);
                var writePerSec = (int)Math.Ceiling((device.WriteCompleted - previousValues[j].WriteCompleted) / timespan);

                Console.WriteLine($"Name:        {device.Name}");
                Console.WriteLine($"ReadPerSec:  {readPerSec}");
                Console.WriteLine($"WritePerSec: {writePerSec}");
            }
        }
    }
}

//--------------------------------------------------------------------------------
// NetworkStat
//--------------------------------------------------------------------------------
[Command("network", "Get network stat")]
public sealed class NetworkCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var network = PlatformProvider.GetNetworkStat();
        foreach (var nif in network.Interfaces)
        {
            Console.WriteLine($"Interface:    {nif.Interface}");
            Console.WriteLine($"RxBytes:      {nif.RxBytes}");
            Console.WriteLine($"RxPackets:    {nif.RxPackets}");
            Console.WriteLine($"RxErrors:     {nif.RxErrors}");
            Console.WriteLine($"RxDropped:    {nif.RxDropped}");
            Console.WriteLine($"RxFifo:       {nif.RxFifo}");
            Console.WriteLine($"RxFrame:      {nif.RxFrame}");
            Console.WriteLine($"RxCompressed: {nif.RxCompressed}");
            Console.WriteLine($"RxMulticast:  {nif.RxMulticast}");
            Console.WriteLine($"TxBytes:      {nif.TxBytes}");
            Console.WriteLine($"TxPackets:    {nif.TxPackets}");
            Console.WriteLine($"TxErrors:     {nif.TxErrors}");
            Console.WriteLine($"TxDropped:    {nif.TxDropped}");
            Console.WriteLine($"TxFifo:       {nif.TxFifo}");
            Console.WriteLine($"TxCollisions: {nif.TxCollisions}");
            Console.WriteLine($"TxCarrier:    {nif.TxCarrier}");
            Console.WriteLine($"TxCompressed: {nif.TxCompressed}");
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Tcp
//--------------------------------------------------------------------------------
[Command("tcp", "Get tcp stat")]
public sealed class TcpCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var tcp = PlatformProvider.GetTcpStat();

        Console.WriteLine($"Established: {tcp.Established}");
        Console.WriteLine($"SynSent:     {tcp.SynSent}");
        Console.WriteLine($"SynRecv:     {tcp.SynRecv}");
        Console.WriteLine($"FinWait1:    {tcp.FinWait1}");
        Console.WriteLine($"FinWait2:    {tcp.FinWait2}");
        Console.WriteLine($"TimeWait:    {tcp.TimeWait}");
        Console.WriteLine($"Close:       {tcp.Close}");
        Console.WriteLine($"CloseWait:   {tcp.CloseWait}");
        Console.WriteLine($"LastAck:     {tcp.LastAck}");
        Console.WriteLine($"Listen:      {tcp.Listen}");
        Console.WriteLine($"Closing:     {tcp.Closing}");
        Console.WriteLine($"Total:       {tcp.Total}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Tcp6
//--------------------------------------------------------------------------------
[Command("tcp6", "Get tcp6 stat")]
public sealed class Tcp6Command : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var tcp = PlatformProvider.GetTcp6Stat();

        Console.WriteLine($"Established: {tcp.Established}");
        Console.WriteLine($"SynSent:     {tcp.SynSent}");
        Console.WriteLine($"SynRecv:     {tcp.SynRecv}");
        Console.WriteLine($"FinWait1:    {tcp.FinWait1}");
        Console.WriteLine($"FinWait2:    {tcp.FinWait2}");
        Console.WriteLine($"TimeWait:    {tcp.TimeWait}");
        Console.WriteLine($"Close:       {tcp.Close}");
        Console.WriteLine($"CloseWait:   {tcp.CloseWait}");
        Console.WriteLine($"LastAck:     {tcp.LastAck}");
        Console.WriteLine($"Listen:      {tcp.Listen}");
        Console.WriteLine($"Closing:     {tcp.Closing}");
        Console.WriteLine($"Total:       {tcp.Total}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// WirelessStat
//--------------------------------------------------------------------------------
[Command("wireless", "Get wireless stat")]
public sealed class WirelessCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var wireless = PlatformProvider.GetWirelessStat();
        foreach (var wif in wireless.Interfaces)
        {
            Console.WriteLine($"Interface:         {wif.Interface}");
            Console.WriteLine($"Status:            {wif.Status:X4}");
            Console.WriteLine($"LinkQuality:       {wif.LinkQuality}");
            Console.WriteLine($"SignalLevel:       {wif.SignalLevel}");
            Console.WriteLine($"NoiseLevel:        {wif.NoiseLevel}");
            Console.WriteLine($"DiscardedNetworkId:{wif.DiscardedNetworkId}");
            Console.WriteLine($"DiscardedCrypt:    {wif.DiscardedCrypt}");
            Console.WriteLine($"DiscardedFragment: {wif.DiscardedFragment}");
            Console.WriteLine($"DiscardedRetry:    {wif.DiscardedRetry}");
            Console.WriteLine($"DiscardedMisc:     {wif.DiscardedMisc}");
            Console.WriteLine($"MissedBeacon:      {wif.MissedBeacon}");
            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// ProcessSummary
//--------------------------------------------------------------------------------
[Command("process", "Get process")]
public sealed class ProcessCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var process = PlatformProvider.GetProcessSummary();

        Console.WriteLine($"ProcessCount: {process.ProcessCount}");
        Console.WriteLine($"ThreadCount:  {process.ThreadCount}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Processes
//--------------------------------------------------------------------------------
[Command("processes", "Get all processes")]
public sealed class ProcessesCommand : ICommandHandler
{
    [Option<int>("--top", "-t", Description = "Top", DefaultValue = 100)]
    public int Top { get; set; }

    [Option<string>("--sort", "-s", Description = "Sort", Completions = ["pid", "name", "cpu", "memory"], DefaultValue = "pid")]
    public string Sort { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var processes = PlatformProvider.GetProcesses();

#pragma warning disable CA1308
        var sorted = Sort.ToLowerInvariant() switch
        {
            "name" => processes.OrderBy(p => p.Name),
            "cpu" => processes.OrderByDescending(p => p.UserTime + p.SystemTime),
            "memory" => processes.OrderByDescending(p => p.ResidentSize),
            _ => processes.OrderBy(p => p.ProcessId)
        };
#pragma warning restore CA1308

        var topProcesses = sorted.Take(Top).ToList();

        Console.WriteLine($"{"PID",-6} {"Name",-20} {"State",-12} {"User",-5} {"Threads",7} {"RSS (MB)",10} {"CPU Time",10}");
        Console.WriteLine(new string('-', 76));

        foreach (var p in topProcesses)
        {
            var rss = (double)p.ResidentSize / 1024 / 1024;
            var cpuTime = (p.UserTime + p.SystemTime) / 100.0;

            Console.WriteLine($"{p.ProcessId,-6} {TruncateName(p.Name, 20),-20} {p.State,-12} {p.UserId,-5} {p.ThreadCount,7} {rss,10:F2} {cpuTime,10:F2}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total processes: {processes.Count}");

        return ValueTask.CompletedTask;
    }

    private static string TruncateName(string name, int maxLength)
    {
        return name.Length <= maxLength ? name : name[..(maxLength - 3)] + "...";
    }
}

//--------------------------------------------------------------------------------
// FileDescriptor
//--------------------------------------------------------------------------------
[Command("fd", "Get file descriptor")]
public sealed class FdCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var fd = PlatformProvider.GetFileHandleStat();

        Console.WriteLine($"Allocated: {fd.Allocated}");
        Console.WriteLine($"Used:      {fd.Used}");
        Console.WriteLine($"Max:       {fd.Max}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Cpu
//--------------------------------------------------------------------------------
[Command("cpu", "Get cpu device")]
public sealed class CpuCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var cpu = PlatformProvider.GetCpuDevice();

        Console.WriteLine("Frequency");
        foreach (var core in cpu.Cores)
        {
            Console.WriteLine($"{core.Name}: {core.Frequency}");
        }

        if (cpu.Powers.Count > 0)
        {
            Console.WriteLine("Power");
            foreach (var power in cpu.Powers)
            {
                Console.WriteLine($"{power.Name}: {power.Energy / 1000.0}");
            }
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// MainsAdapter
//--------------------------------------------------------------------------------
[Command("ac", "Get A/C device")]
public sealed class AcCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var adapter = PlatformProvider.GetMainsDevice();

        Console.WriteLine(adapter.Supported ? $"Online: {adapter.Online}" : "No adapter found");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Battery
//--------------------------------------------------------------------------------
[Command("battery", "Get battery device")]
public sealed class BatteryCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var battery = PlatformProvider.GetBatteryDevice();

        if (battery.Supported)
        {
            Console.WriteLine($"Capacity:   {battery.Capacity}");
            Console.WriteLine($"Status:     {battery.Status}");
            Console.WriteLine($"Voltage:    {battery.Voltage / 1000.0:F2}");
            Console.WriteLine($"Current:    {battery.Current / 1000.0:F2}");
            Console.WriteLine($"Charge:     {battery.Charge / 1000.0:F2}");
            Console.WriteLine($"ChargeFull: {battery.ChargeFull / 1000.0:F2}");
        }
        else
        {
            Console.WriteLine("No battery found");
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// HardwareMonitor
//--------------------------------------------------------------------------------
[Command("hwmon", "Get hardware monitors")]
public sealed class HwmonCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var monitors = PlatformProvider.GetHardwareMonitors();
        foreach (var monitor in monitors)
        {
            Console.WriteLine($"Monitor: {monitor.Type}");
            Console.WriteLine($"Name:    {monitor.Name}");
            foreach (var sensor in monitor.Sensors)
            {
                Console.WriteLine($"Sensor:  {sensor.Type}");
                Console.WriteLine($"Label:   {sensor.Label}");
                Console.WriteLine($"Value:   {sensor.Value}");
            }

            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }
}
