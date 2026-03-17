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

public static class DisplayFormatter
{
    public static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 40 => $"{bytes / (double)(1UL << 40):F2} TB",
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F2} GB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F2} MB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F2} KB",
        _ => $"{bytes} B"
    };

    public static string MakeBar(double value, double max, int width = 20)
    {
        ReadOnlySpan<char> partials = ['▏', '▎', '▍', '▌', '▋', '▊', '▉'];

        var ratio = max > 0 ? Math.Clamp(value / max, 0.0, 1.0) : 0.0;
        var totalEighths = (int)Math.Round(ratio * width * 8);
        var fullCells = totalEighths / 8;
        var remainder = totalEighths % 8;

        var buf = new char[width + 2];
        buf[0] = '[';
        buf[width + 1] = ']';

        var pos = 1;
        for (var i = 0; i < fullCells; i++)
        {
            buf[pos++] = '█';
        }

        if (remainder > 0 && fullCells < width)
        {
            buf[pos++] = partials[remainder - 1];
        }

        while (pos <= width)
        {
            buf[pos++] = ' ';
        }

        return new string(buf);
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
        Console.WriteLine($"L1D:            {DisplayFormatter.FormatBytes((ulong)hw.L1DCacheSize)}");
        Console.WriteLine($"L1I:            {DisplayFormatter.FormatBytes((ulong)hw.L1ICacheSize)}");
        Console.WriteLine($"L2:             {DisplayFormatter.FormatBytes((ulong)hw.L2CacheSize)}");
        Console.WriteLine($"L3:             {DisplayFormatter.FormatBytes((ulong)hw.L3CacheSize)}");

        Console.WriteLine("[Memory]");
        Console.WriteLine($"MemoryTotal:    {DisplayFormatter.FormatBytes((ulong)hw.MemoryTotal)}");
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

        Console.WriteLine($"OsType:             {kernel.OsType}");
        Console.WriteLine($"OsRelease:          {kernel.OsRelease}");
        Console.WriteLine($"KernelVersion:      {kernel.KernelVersion}");
        Console.WriteLine($"OsProductVersion:   {kernel.OsProductVersion}");
        Console.WriteLine($"OsName:             {kernel.OsName}");
        Console.WriteLine($"OsPrettyName:       {kernel.OsPrettyName}");
        Console.WriteLine($"OsId:               {kernel.OsId}");
        Console.WriteLine($"MaxProcesses:       {kernel.MaxProcesses}");
        Console.WriteLine($"MaxFiles:           {kernel.MaxFiles}");
        Console.WriteLine($"MaxFilesPerProcess: {kernel.MaxFilesPerProcess}");
        Console.WriteLine($"BootTime:           {kernel.BootTime:yyyy-MM-dd HH:mm:ss zzz}");

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

        var elapsed = uptime.Elapsed;
        Console.WriteLine($"Uptime: {(int)elapsed.TotalDays}d {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}");

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

        using var cts = new CancellationTokenSource();
#pragma warning disable SA1107
        // ReSharper disable once AccessToDisposedClosure
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
#pragma warning restore SA1107

        while (!cts.Token.IsCancellationRequested)
        {
            var previousValues = stat.CpuCores
                .Select(static x => (Idle: CalcCpuIdle(x), Total: CalcCpuTotal(x)))
                .ToList();

            await Task.Delay(1000, cts.Token);

            stat.Update();

            Console.Clear();
            for (var j = 0; j < stat.CpuCores.Count; j++)
            {
                var core = stat.CpuCores[j];
                var idle = CalcCpuIdle(core);
                var total = CalcCpuTotal(core);

                var idleDiff = idle - previousValues[j].Idle;
                var totalDiff = total - previousValues[j].Total;
                var usage = totalDiff > 0 ? (int)Math.Ceiling((double)(totalDiff - idleDiff) / totalDiff * 100d) : 0;

                Console.WriteLine($"{core.Name}: {DisplayFormatter.MakeBar(usage, 100)} {usage,3}%");
            }

            Console.WriteLine();

            Console.WriteLine($"Interrupt:       {stat.Interrupt}");
            Console.WriteLine($"ContextSwitch:   {stat.ContextSwitch}");
            Console.WriteLine($"SoftIrq:         {stat.SoftIrq}");
            Console.WriteLine($"Forks:           {stat.Forks}");
            Console.WriteLine($"RunnableTasks:   {stat.RunnableTasks}");
            Console.WriteLine($"BlockedTasks:    {stat.BlockedTasks}");

            Console.WriteLine($"Total User:      {stat.CpuCores.Sum(static x => x.User)}");
            Console.WriteLine($"Total Nice:      {stat.CpuCores.Sum(static x => x.Nice)}");
            Console.WriteLine($"Total System:    {stat.CpuCores.Sum(static x => x.System)}");
            Console.WriteLine($"Total Idle:      {stat.CpuCores.Sum(static x => x.Idle)}");
            Console.WriteLine($"Total IoWait:    {stat.CpuCores.Sum(static x => x.IoWait)}");
            Console.WriteLine($"Total Irq:       {stat.CpuCores.Sum(static x => x.Irq)}");
            Console.WriteLine($"Total SoftIrq:   {stat.CpuCores.Sum(static x => x.SoftIrq)}");
            Console.WriteLine($"Total Steal:     {stat.CpuCores.Sum(static x => x.Steal)}");
            Console.WriteLine($"Total Guest:     {stat.CpuCores.Sum(static x => x.Guest)}");
            Console.WriteLine($"Total GuestNice: {stat.CpuCores.Sum(static x => x.GuestNice)}");
        }

        static long CalcCpuIdle(CpuStat cpu) =>
            cpu.Idle + cpu.IoWait;

        static long CalcCpuTotal(CpuStat cpu) =>
            cpu.User + cpu.Nice + cpu.System + cpu.Irq + cpu.SoftIrq + cpu.Steal + cpu.Idle + cpu.IoWait;
    }
}

//--------------------------------------------------------------------------------
// LoadAverage
//--------------------------------------------------------------------------------
[Command("load", "Get load average")]
public sealed class LoadCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var cpuCount = Environment.ProcessorCount;

        using var cts = new CancellationTokenSource();
#pragma warning disable SA1107
        // ReSharper disable once AccessToDisposedClosure
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
#pragma warning restore SA1107

        while (!cts.Token.IsCancellationRequested)
        {
            var load = PlatformProvider.GetLoadAverage();

            Console.Clear();
            Console.WriteLine($"Load Average (CPUs: {cpuCount})");
            Console.WriteLine($"   1 min: {DisplayFormatter.MakeBar(load.Average1, cpuCount)} {load.Average1:F2}");
            Console.WriteLine($"   5 min: {DisplayFormatter.MakeBar(load.Average5, cpuCount)} {load.Average5:F2}");
            Console.WriteLine($"  15 min: {DisplayFormatter.MakeBar(load.Average15, cpuCount)} {load.Average15:F2}");

            await Task.Delay(2000, cts.Token);
        }
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
        var used = memory.MemoryTotal - memory.MemoryAvailable;
        var usagePct = memory.MemoryTotal > 0 ? (double)used / memory.MemoryTotal * 100 : 0;

        Console.WriteLine("[Usage]");
        Console.WriteLine($"  Total:     {DisplayFormatter.FormatBytes((ulong)(memory.MemoryTotal * 1024L))}");
        Console.WriteLine($"  Used:      {DisplayFormatter.FormatBytes((ulong)(used * 1024L))} ({usagePct:F1}%) {DisplayFormatter.MakeBar(used, memory.MemoryTotal)}");
        Console.WriteLine($"  Available: {DisplayFormatter.FormatBytes((ulong)(memory.MemoryAvailable * 1024L))}");
        Console.WriteLine($"  Free:      {DisplayFormatter.FormatBytes((ulong)(memory.MemoryFree * 1024L))}");

        Console.WriteLine("[Buffers/Cache]");
        Console.WriteLine($"  Buffers:   {DisplayFormatter.FormatBytes((ulong)(memory.Buffers * 1024L))}");
        Console.WriteLine($"  Cached:    {DisplayFormatter.FormatBytes((ulong)(memory.Cached * 1024L))}");

        if (memory.SwapTotal > 0)
        {
            var swapUsed = memory.SwapTotal - memory.SwapFree;
            var swapPct = (double)swapUsed / memory.SwapTotal * 100;
            Console.WriteLine("[Swap]");
            Console.WriteLine($"  Total:     {DisplayFormatter.FormatBytes((ulong)(memory.SwapTotal * 1024L))}");
            Console.WriteLine($"  Used:      {DisplayFormatter.FormatBytes((ulong)(swapUsed * 1024L))} ({swapPct:F1}%) {DisplayFormatter.MakeBar(swapUsed, memory.SwapTotal)}");
            Console.WriteLine($"  Free:      {DisplayFormatter.FormatBytes((ulong)(memory.SwapFree * 1024L))}");
        }

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
            Console.WriteLine($"[{mount.MountPoint}]");
            Console.WriteLine($"  Device:     {mount.DeviceName}");
            Console.WriteLine($"  FileSystem: {mount.FileSystem}");
            Console.WriteLine($"  Options:    {mount.Option}");
            Console.WriteLine($"  IsLocal:    {mount.IsLocal}");

            var usage = PlatformProvider.GetFileSystemUsage(mount.MountPoint);
            var used = usage.TotalSize > usage.FreeSize ? usage.TotalSize - usage.FreeSize : 0UL;
            var usagePercent = (used + usage.AvailableSize) > 0
                ? (double)used / (used + usage.AvailableSize) * 100
                : 0;
            Console.WriteLine($"  Usage:      {usagePercent:F1}% ({DisplayFormatter.FormatBytes(used)} / {DisplayFormatter.FormatBytes(usage.TotalSize)}) {DisplayFormatter.MakeBar(usagePercent, 100)}");
            Console.WriteLine($"  Available:  {DisplayFormatter.FormatBytes(usage.AvailableSize)}");
            Console.WriteLine($"  Files:      {usage.FreeFiles} free / {usage.TotalFiles} total");
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

        using var cts = new CancellationTokenSource();
#pragma warning disable SA1107
        // ReSharper disable once AccessToDisposedClosure
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
#pragma warning restore SA1107

        while (!cts.Token.IsCancellationRequested)
        {
            var previousUpdateAt = disk.UpdateAt;
            var previousValues = disk.Devices
                .Select(x => new { x.ReadCompleted, x.WriteCompleted, x.ReadSectors, x.WriteSectors })
                .ToList();

            await Task.Delay(1000, cts.Token);

            disk.Update();

            var timespan = (disk.UpdateAt - previousUpdateAt).TotalSeconds;

            Console.Clear();
            for (var j = 0; j < disk.Devices.Count; j++)
            {
                var device = disk.Devices[j];
                var readPerSec = timespan > 0 ? (device.ReadCompleted - previousValues[j].ReadCompleted) / timespan : 0;
                var writePerSec = timespan > 0 ? (device.WriteCompleted - previousValues[j].WriteCompleted) / timespan : 0;
                var readBytesPerSec = timespan > 0 ? (device.ReadSectors - previousValues[j].ReadSectors) * 512.0 / timespan : 0;
                var writeBytesPerSec = timespan > 0 ? (device.WriteSectors - previousValues[j].WriteSectors) * 512.0 / timespan : 0;

                Console.WriteLine($"[{device.Name}]");
                Console.WriteLine($"  Read:  {readPerSec,6:F0} IOPS  {DisplayFormatter.FormatBytes((ulong)Math.Max(0.0, readBytesPerSec))}/s");
                Console.WriteLine($"  Write: {writePerSec,6:F0} IOPS  {DisplayFormatter.FormatBytes((ulong)Math.Max(0.0, writeBytesPerSec))}/s");
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
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var network = PlatformProvider.GetNetworkStat();

        using var cts = new CancellationTokenSource();
#pragma warning disable SA1107
        // ReSharper disable once AccessToDisposedClosure
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
#pragma warning restore SA1107

        while (!cts.Token.IsCancellationRequested)
        {
            var snapshot = network.Interfaces
                .ToDictionary(x => x.Interface, x => (x.RxBytes, x.RxPackets, x.RxErrors, x.TxBytes, x.TxPackets, x.TxErrors));
            var t0 = DateTime.UtcNow;

            await Task.Delay(1000, cts.Token);

            network.Update();
            var elapsed = (DateTime.UtcNow - t0).TotalSeconds;

            Console.Clear();
            foreach (var nif in network.Interfaces)
            {
                snapshot.TryGetValue(nif.Interface, out var prev);
                var deltaRxBytes = unchecked(nif.RxBytes - prev.RxBytes);
                var deltaRxPackets = unchecked(nif.RxPackets - prev.RxPackets);
                var deltaRxErrors = unchecked(nif.RxErrors - prev.RxErrors);
                var deltaTxBytes = unchecked(nif.TxBytes - prev.TxBytes);
                var deltaTxPackets = unchecked(nif.TxPackets - prev.TxPackets);
                var deltaTxErrors = unchecked(nif.TxErrors - prev.TxErrors);

                var rxBytesPerSec = elapsed > 0 ? deltaRxBytes / elapsed : 0;
                var txBytesPerSec = elapsed > 0 ? deltaTxBytes / elapsed : 0;

                Console.WriteLine($"[{nif.Interface}]");
                Console.WriteLine($"  RX: {DisplayFormatter.FormatBytes((ulong)Math.Max(0L, nif.RxBytes)),10} total  {DisplayFormatter.FormatBytes((ulong)Math.Max(0.0, rxBytesPerSec))}/s  ({deltaRxPackets} packet/s, {deltaRxErrors} error)");
                Console.WriteLine($"  TX: {DisplayFormatter.FormatBytes((ulong)Math.Max(0L, nif.TxBytes)),10} total  {DisplayFormatter.FormatBytes((ulong)Math.Max(0.0, txBytesPerSec))}/s  ({deltaTxPackets} packet/s, {deltaTxErrors} error)");
            }
        }
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
            "memory" => processes.OrderByDescending(p => p.ResidentMemorySize),
            _ => processes.OrderBy(p => p.ProcessId)
        };
#pragma warning restore CA1308

        var topProcesses = sorted.Take(Top).ToList();

        Console.WriteLine($"{"PID",-6} {"Name",-20} {"State",-12} {"User",-5} {"Threads",7} {"RSS (MB)",10} {"CPU Time",10}");
        Console.WriteLine(new string('-', 76));

        foreach (var p in topProcesses)
        {
            var rss = (double)p.ResidentMemorySize / 1024 / 1024;
            var cpuTime = (p.UserTime + p.SystemTime).TotalSeconds;

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
        var hw = PlatformProvider.GetHardware();
        var maxFreqKHz = hw.CpuFrequencyMax / 1000;
        if (maxFreqKHz == 0 && cpu.Cores.Count > 0)
        {
            maxFreqKHz = cpu.Cores.Max(c => c.Frequency);
        }

        Console.WriteLine("[Frequency]");
        foreach (var core in cpu.Cores)
        {
            var freqMHz = core.Frequency / 1000.0;
            Console.WriteLine($"  {core.Name}: {DisplayFormatter.MakeBar(core.Frequency, maxFreqKHz)} {freqMHz,7:F1} MHz");
        }

        if (cpu.Powers.Count > 0)
        {
            Console.WriteLine("[Power]");
            foreach (var power in cpu.Powers)
            {
                Console.WriteLine($"  {power.Name}: {power.Energy / 1_000_000.0:F2} J");
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
            Console.WriteLine($"Capacity:   {battery.Capacity}% {DisplayFormatter.MakeBar(battery.Capacity, 100)}");
            Console.WriteLine($"Status:     {battery.Status}");
            Console.WriteLine($"Voltage:    {battery.Voltage / 1000.0:F0} mV");
            Console.WriteLine($"Current:    {battery.Current / 1000.0:F0} mA");
            Console.WriteLine($"Charge:     {battery.Charge / 1000.0:F0} mAh");
            Console.WriteLine($"ChargeFull: {battery.ChargeFull / 1000.0:F0} mAh");
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
            var type = String.IsNullOrEmpty(monitor.Type) ? string.Empty : $" ({monitor.Type})";
            Console.WriteLine($"[{monitor.Name}]{type}");
            foreach (var sensor in monitor.Sensors)
            {
                Console.WriteLine($"  {sensor.Label,-16} {FormatSensorValue(sensor.Type, sensor.Value)}");
            }
        }

        return ValueTask.CompletedTask;
    }

    private static string FormatSensorValue(string type, long value) => type switch
    {
        "temp" => $"{value / 1000.0:F1} C",
        "fan" => $"{value} RPM",
        "in" => $"{value / 1000.0:F3} V",
        "power" => $"{value / 1_000_000.0:F2} W",
        "curr" => $"{value / 1000.0:F3} A",
        "energy" => $"{value / 1_000_000.0:F2} J",
        _ => $"{value}"
    };
}
