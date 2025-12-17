#pragma warning disable CA1416
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable UseObjectOrCollectionInitializer
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using LinuxDotNet.SystemInfo;

var rootCommand = new RootCommand("Platform info");

//--------------------------------------------------------------------------------
// Uptime
//--------------------------------------------------------------------------------
var uptimeCommand = new Command("uptime", "Get uptime");
uptimeCommand.Handler = CommandHandler.Create(static () =>
{
    var uptime = PlatformProvider.GetUptime();
    Console.WriteLine($"Uptime: {uptime.Uptime}");
});
rootCommand.Add(uptimeCommand);

//--------------------------------------------------------------------------------
// Statics
//--------------------------------------------------------------------------------
var statCommand = new Command("stat", "Get statics");
statCommand.Handler = CommandHandler.Create(static () =>
{
    var statics = PlatformProvider.GetStatics();
    Console.WriteLine($"Interrupt:      {statics.Interrupt}");
    Console.WriteLine($"ContextSwitch:  {statics.ContextSwitch}");
    Console.WriteLine($"SoftIrq:        {statics.SoftIrq}");
    Console.WriteLine($"ProcessRunning: {statics.ProcessRunning}");
    Console.WriteLine($"ProcessBlocked: {statics.ProcessBlocked}");

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
            .Select(x => new
            {
                x.Active,
                x.Total
            })
            .ToList();

        Thread.Sleep(1000);

        statics.Update();

        for (var j = 0; j < statics.CpuCores.Count; j++)
        {
            var core = statics.CpuCores[j];
            var activeDiff = core.Active - previousValues[j].Active;
            var totalDiff = core.Total - previousValues[j].Total;
            var usage = (int)Math.Ceiling((double)activeDiff / totalDiff * 100.0);

            Console.WriteLine($"Name:  {core.Name}");
            Console.WriteLine($"Usage: {usage}");
        }
    }
});
rootCommand.Add(statCommand);

//--------------------------------------------------------------------------------
// LoadAverage
//--------------------------------------------------------------------------------
var loadCommand = new Command("load", "Get load average");
loadCommand.Handler = CommandHandler.Create(static () =>
{
    var load = PlatformProvider.GetLoadAverage();
    Console.WriteLine($"Average1:  {load.Average1:F2}");
    Console.WriteLine($"Average5:  {load.Average5:F2}");
    Console.WriteLine($"Average15: {load.Average15:F2}");
});
rootCommand.Add(loadCommand);

//--------------------------------------------------------------------------------
// Memory
//--------------------------------------------------------------------------------
var memoryCommand = new Command("memory", "Get memory");
memoryCommand.Handler = CommandHandler.Create(static () =>
{
    var memory = PlatformProvider.GetMemory();
    Console.WriteLine($"Total:   {memory.Total}");
    Console.WriteLine($"Free:    {memory.Free}");
    Console.WriteLine($"Buffers: {memory.Buffers}");
    Console.WriteLine($"Cached:  {memory.Cached}");
    Console.WriteLine($"Usage:   {(int)Math.Ceiling(memory.Usage)}");
});
rootCommand.Add(memoryCommand);

//--------------------------------------------------------------------------------
// VirtualMemory
//--------------------------------------------------------------------------------
var virtualCommand = new Command("virtual", "Get virtual memory");
virtualCommand.Handler = CommandHandler.Create(static () =>
{
    var vm = PlatformProvider.GetVirtualMemory();
    Console.WriteLine($"PageIn:            {vm.PageIn}");
    Console.WriteLine($"PageOut:           {vm.PageOut}");
    Console.WriteLine($"SwapIn:            {vm.SwapIn}");
    Console.WriteLine($"SwapOut:           {vm.SwapOut}");
    Console.WriteLine($"PageFault:         {vm.PageFault}");
    Console.WriteLine($"MajorPageFault:    {vm.MajorPageFault}");
    Console.WriteLine($"OutOfMemoryKiller: {vm.OutOfMemoryKiller}");
});
rootCommand.Add(virtualCommand);

//--------------------------------------------------------------------------------
// Partition
//--------------------------------------------------------------------------------
var partitionCommand = new Command("partition", "Get parition");
partitionCommand.Handler = CommandHandler.Create(static () =>
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
});
rootCommand.Add(partitionCommand);

//--------------------------------------------------------------------------------
// DiskStatics
//--------------------------------------------------------------------------------
var diskCommand = new Command("disk", "Get disk statics");
diskCommand.Handler = CommandHandler.Create(static () =>
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

        Thread.Sleep(1000);

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
});
rootCommand.Add(diskCommand);

//--------------------------------------------------------------------------------
// FileDescriptor
//--------------------------------------------------------------------------------
var fdCommand = new Command("fd", "Get file descriptor");
fdCommand.Handler = CommandHandler.Create(static () =>
{
    var fd = PlatformProvider.GetFileDescriptor();
    Console.WriteLine($"Allocated: {fd.Allocated}");
    Console.WriteLine($"Used:      {fd.Used}");
    Console.WriteLine($"Max:       {fd.Max}");
});
rootCommand.Add(fdCommand);

//--------------------------------------------------------------------------------
// NetworkStatic
//--------------------------------------------------------------------------------
var networkCommand = new Command("network", "Get network statics");
networkCommand.Handler = CommandHandler.Create(static () =>
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
});
rootCommand.Add(networkCommand);

//--------------------------------------------------------------------------------
// Tcp
//--------------------------------------------------------------------------------
var tcpCommand = new Command("tcp", "Get tcp");
tcpCommand.Handler = CommandHandler.Create(static () =>
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
});
rootCommand.Add(tcpCommand);

//--------------------------------------------------------------------------------
// Tcp6
//--------------------------------------------------------------------------------
var tcp6Command = new Command("tcp6", "Get tcp6");
tcp6Command.Handler = CommandHandler.Create(static () =>
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
});
rootCommand.Add(tcp6Command);

//--------------------------------------------------------------------------------
// ProcessSummary
//--------------------------------------------------------------------------------
var processCommand = new Command("process", "Get process");
processCommand.Handler = CommandHandler.Create(static () =>
{
    var process = PlatformProvider.GetProcessSummary();
    Console.WriteLine($"ProcessCount: {process.ProcessCount}");
    Console.WriteLine($"ThreadCount:  {process.ThreadCount}");
});
rootCommand.Add(processCommand);

//--------------------------------------------------------------------------------
// Cpu
//--------------------------------------------------------------------------------
var cpuCommand = new Command("cpu", "Get cpu");
cpuCommand.Handler = CommandHandler.Create(static () =>
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
});
rootCommand.Add(cpuCommand);

//--------------------------------------------------------------------------------
// Battery
//--------------------------------------------------------------------------------
var batteryCommand = new Command("battery", "Get battery");
batteryCommand.Handler = CommandHandler.Create(static () =>
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
});
rootCommand.Add(batteryCommand);

//--------------------------------------------------------------------------------
// MainsAdapter
//--------------------------------------------------------------------------------
var acCommand = new Command("ac", "Get ac");
acCommand.Handler = CommandHandler.Create(static () =>
{
    var adapter = PlatformProvider.GetMainsAdapter();
    if (adapter.Supported)
    {
        Console.WriteLine($"Online: {adapter.Online}");
    }
    else
    {
        Console.WriteLine("No adapter found");
    }
});
rootCommand.Add(acCommand);

//--------------------------------------------------------------------------------
// HardwareMonitor
//--------------------------------------------------------------------------------
var hwmonCommand = new Command("hwmon", "Get hwmon");
hwmonCommand.Handler = CommandHandler.Create(static () =>
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
});
rootCommand.Add(hwmonCommand);

//--------------------------------------------------------------------------------
// Run
//--------------------------------------------------------------------------------
rootCommand.Invoke(args);
