// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Example.SystemInfo.ConsoleApp;

using LinuxDotNet.SystemInfo;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<UptimeCommand>();
        commands.AddCommand<StatCommand>();
        commands.AddCommand<LoadCommand>();
        commands.AddCommand<MemoryCommand>();
        commands.AddCommand<VirtualCommand>();
        commands.AddCommand<PartitionCommand>();
        commands.AddCommand<DiskCommand>();
        commands.AddCommand<FdCommand>();
        commands.AddCommand<NetworkCommand>();
        commands.AddCommand<TcpCommand>();
        commands.AddCommand<Tcp6Command>();
        commands.AddCommand<ProcessCommand>();
        commands.AddCommand<CpuCommand>();
        commands.AddCommand<BatteryCommand>();
        commands.AddCommand<AcCommand>();
        commands.AddCommand<HwmonCommand>();
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

        Console.WriteLine($"Uptime: {uptime.Uptime}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Statics
//--------------------------------------------------------------------------------
[Command("stat", "Get statics")]
public sealed class StatCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var statics = PlatformProvider.GetStatics();

        Console.WriteLine($"Interrupt:      {statics.Interrupt}");
        Console.WriteLine($"ContextSwitch:  {statics.ContextSwitch}");
        Console.WriteLine($"SoftIrq:        {statics.SoftIrq}");
        Console.WriteLine($"Forks:          {statics.Forks}");
        Console.WriteLine($"RunnableTasks:  {statics.RunnableTasks}");
        Console.WriteLine($"BlockedTasks:   {statics.BlockedTasks}");

        Console.WriteLine($"User:           {statics.CpuTotal.User}");
        Console.WriteLine($"Nice:           {statics.CpuTotal.Nice}");
        Console.WriteLine($"System:         {statics.CpuTotal.System}");
        Console.WriteLine($"Idle:           {statics.CpuTotal.Idle}");
        Console.WriteLine($"IoWait:         {statics.CpuTotal.IoWait}");
        Console.WriteLine($"Irq:            {statics.CpuTotal.Irq}");
        Console.WriteLine($"SoftIrq:        {statics.CpuTotal.SoftIrq}");
        Console.WriteLine($"Steal:          {statics.CpuTotal.Steal}");
        Console.WriteLine($"Guest:          {statics.CpuTotal.Guest}");
        Console.WriteLine($"GuestNice:      {statics.CpuTotal.GuestNice}");

        Console.WriteLine();

        for (var i = 0; i < 10; i++)
        {
            var previousValues = statics.CpuCores
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

            statics.Update();

            for (var j = 0; j < statics.CpuCores.Count; j++)
            {
                var core = statics.CpuCores[j];
                var nonIdle = CalcCpuNonIdle(core);
                var total = nonIdle + CalcCpuIdle(core);

                var nonIdleDiff = nonIdle - previousValues[j].NonIdle;
                var totalDiff = total - previousValues[j].Total;
                var usage = totalDiff > 0 ? (int)Math.Ceiling((double)nonIdleDiff / totalDiff * 100.0) : 0;

                Console.WriteLine($"Name:  {core.Name}");
                Console.WriteLine($"Usage: {usage}");
            }
        }

        static long CalcCpuIdle(CpuStatics cpu)
        {
            return cpu.Idle + cpu.IoWait;
        }

        static long CalcCpuNonIdle(CpuStatics cpu)
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
[Command("memory", "Get memory")]
public sealed class MemoryCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var memory = PlatformProvider.GetMemory();
        var usage = (int)Math.Ceiling((double)(memory.MemTotal - memory.MemAvailable) / memory.MemTotal * 100);

        Console.WriteLine($"MemTotal:     {memory.MemTotal}");
        Console.WriteLine($"MemAvailable: {memory.MemAvailable}");
        Console.WriteLine($"Buffers:      {memory.Buffers}");
        Console.WriteLine($"Cached:       {memory.Cached}");
        Console.WriteLine($"Usage:        {usage}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// VirtualMemory
//--------------------------------------------------------------------------------
[Command("virtual", "Get virtual memory")]
public sealed class VirtualCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var vm = PlatformProvider.GetVirtualMemory();

        Console.WriteLine($"PageIn:            {vm.PageIn}");
        Console.WriteLine($"PageOut:           {vm.PageOut}");
        Console.WriteLine($"SwapIn:            {vm.SwapIn}");
        Console.WriteLine($"SwapOut:           {vm.SwapOut}");
        Console.WriteLine($"PageFault:         {vm.PageFault}");
        Console.WriteLine($"MajorPageFault:    {vm.MajorPageFault}");
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
            var drive = new DriveInfo(partition.MountPoints[0]);
            var used = drive.TotalSize - drive.TotalFreeSpace;
            var available = drive.AvailableFreeSpace;
            var usage = (int)Math.Ceiling((double)used / (available + used) * 100);

            Console.WriteLine($"Name:          {partition.Name}");
            Console.WriteLine($"MountPoint:    {String.Join(' ', partition.MountPoints)}");
            Console.WriteLine($"TotalSize:     {drive.TotalSize / 1024}");
            Console.WriteLine($"UsedSize:      {used / 1024}");
            Console.WriteLine($"AvailableSize: {available / 1024}");
            Console.WriteLine($"Usage:         {usage}");
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// DiskStatics
//--------------------------------------------------------------------------------
[Command("disk", "Get disk statics")]
public sealed class DiskCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var disk = PlatformProvider.GetDiskStatics();
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
// FileDescriptor
//--------------------------------------------------------------------------------
[Command("fd", "Get file descriptor")]
public sealed class FdCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var fd = PlatformProvider.GetFileDescriptor();

        Console.WriteLine($"Allocated: {fd.Allocated}");
        Console.WriteLine($"Used:      {fd.Used}");
        Console.WriteLine($"Max:       {fd.Max}");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// NetworkStatic
//--------------------------------------------------------------------------------
[Command("network", "Get network statics")]
public sealed class NetworkCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var network = PlatformProvider.GetNetworkStatic();
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
[Command("tcp", "Get tcp")]
public sealed class TcpCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var tcp = PlatformProvider.GetTcp();

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
[Command("tcp6", "Get tcp6")]
public sealed class Tcp6Command : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var tcp = PlatformProvider.GetTcp6();

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
// Cpu
//--------------------------------------------------------------------------------
[Command("cpu", "Get cpu")]
public sealed class CpuCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var cpu = PlatformProvider.GetCpu();

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
// Battery
//--------------------------------------------------------------------------------
[Command("battery", "Get battery")]
public sealed class BatteryCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var battery = PlatformProvider.GetBattery();

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
// MainsAdapter
//--------------------------------------------------------------------------------
[Command("ac", "Get ac")]
public sealed class AcCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var adapter = PlatformProvider.GetMainsAdapter();

        Console.WriteLine(adapter.Supported ? $"Online: {adapter.Online}" : "No adapter found");

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// HardwareMonitor
//--------------------------------------------------------------------------------
[Command("hwmon", "Get hwmon")]
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
        }

        return ValueTask.CompletedTask;
    }
}
