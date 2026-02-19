# Linux platform library for .NET

|Library|NuGet|
|:----|:----|
|LinuxDotNet.Cups|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Cups.svg)](https://www.nuget.org/packages/LinuxDotNet.Cups)|
|LinuxDotNet.Disk|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Disk.svg)](https://www.nuget.org/packages/LinuxDotNet.Disk)|
|LinuxDotNet.GameInput|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.GameInput.svg)](https://www.nuget.org/packages/LinuxDotNet.GameInput)|
|LinuxDotNet.InputEvent|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.InputEvent.svg)](https://www.nuget.org/packages/LinuxDotNet.InputEvent)|
|LinuxDotNet.SystemInfo|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.SystemInfo.svg)](https://www.nuget.org/packages/LinuxDotNet.SystemInfo)|
|LinuxDotNet.Video4Linux2|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Video4Linux2.svg)](https://www.nuget.org/packages/LinuxDotNet.Video4Linux2)|

![Video2](https://github.com/usausa/linux-dotnet/blob/main/Document/video2.png)

# 🖨️LinuxDotNet.Cups

CUPS API wrapper.

## Usage

### List printers

```csharp
foreach (var printer in CupsPrinter.GetPrinters())
{
    Console.Write(printer.Name);
    if (printer.IsDefault)
    {
        Console.Write(" (Default)");
    }
    Console.WriteLine();

    foreach (var (name, value) in printer.Options)
    {
        Console.WriteLine($"  {name}: {value}");
    }
}
```

### Print file

```csharp
var jobId = CupsPrinter.PrintFile(file, printer);
```

### Print stream

```csharp
using var image = SampleImage.Create();
var options = new PrintOptions
{
    Printer = printer,
    Copies = 1,
    MediaSize = "A4",
    ColorMode = true,
    Orientation = PrintOrientation.Portrait,
    Quality = PrintQuality.Normal
};

var jobId = CupsPrinter.PrintStream(image, options);
```

# ⚡LinuxDotNet.Disk

SMART infotmation.

## Usage

### List printers

```csharp
var disks = DiskInfo.GetInformation();
foreach (var disk in disks)
{
    Console.WriteLine($"Disk #{disk.Index}: {disk.DeviceName}");
    Console.WriteLine($"  Model:          {disk.Model}");
    Console.WriteLine($"  Serial:         {disk.SerialNumber}");
    Console.WriteLine($"  Firmware:       {disk.FirmwareRevision}");
    Console.WriteLine($"  DiskType:       {disk.DiskType}");
    Console.WriteLine($"  SmartType:      {disk.SmartType}");
    Console.WriteLine($"  Size:           {FormatSize(disk.Size)}");
    Console.WriteLine($"  Removable:      {disk.Removable}");

    var partitions = disk.GetPartitions().ToList();
    if (partitions.Count > 0)
    {
        Console.WriteLine("  Partitions:");
        foreach (var partition in partitions)
        {
            var mountInfo = !String.IsNullOrEmpty(partition.MountPoint) ? $" -> {partition.MountPoint} ({partition.FileSystem})" : string.Empty;
            Console.WriteLine($"    {partition.Name}: {FormatSize(partition.Size)}{mountInfo}");
        }
    }

    if (disk.SmartType == SmartType.Nvme)
    {
        PrintNvmeSmart((ISmartNvme)disk.Smart);
    }
    else if (disk.SmartType == SmartType.Generic)
    {
        PrintGenericSmart((ISmartGeneric)disk.Smart);
    }
}

static void PrintNvmeSmart(ISmartNvme smart)
{
    Console.WriteLine($"  SMART (NVMe): Update=[{smart.LastUpdate}]");
    Console.WriteLine($"    CriticalWarning:          {smart.CriticalWarning}");
    Console.WriteLine($"    Temperature:              {smart.Temperature}C");
    Console.WriteLine($"    AvailableSpare:           {smart.AvailableSpare}%");
    Console.WriteLine($"    PercentageUsed:           {smart.PercentageUsed}%");
    Console.WriteLine($"    DataUnitRead:             {smart.DataUnitRead} ({FormatDataUnits(smart.DataUnitRead)})");
    Console.WriteLine($"    DataUnitWritten:          {smart.DataUnitWritten} ({FormatDataUnits(smart.DataUnitWritten)})");
    Console.WriteLine($"    PowerCycles:              {smart.PowerCycles}");
    Console.WriteLine($"    PowerOnHours:             {smart.PowerOnHours}");
    Console.WriteLine($"    UnsafeShutdowns:          {smart.UnsafeShutdowns}");
    Console.WriteLine($"    MediaErrors:              {smart.MediaErrors}");
}

static void PrintGenericSmart(ISmartGeneric smart)
{
    Console.WriteLine($"  SMART (Generic): Update=[{smart.LastUpdate}]");
    Console.WriteLine("    ID   FLAG   CUR  WOR  RAW");
    Console.WriteLine("    ---  ----   ---  ---  --------");

    foreach (var id in smart.GetSupportedIds())
    {
        var attr = smart.GetAttribute(id);
        if (attr.HasValue)
        {
            Console.WriteLine($"    {(byte)id,3}  0x{attr.Value.Flags:X4} {attr.Value.CurrentValue,3}  {attr.Value.WorstValue,3}  {attr.Value.RawValue}");
        }
    }
}
```

# 🎮LinuxDotNet.GameInput

`/dev/input/js*` device reader.

## Usage

### Use event

```csharp
using var controller = new GameController();

controller.ConnectionChanged += static connected =>
{
    Console.WriteLine($"Connected: {connected}");
};
controller.ButtonChanged += static (address, value) =>
{
    Console.WriteLine($"Button {address} Changed: {value}");
};
controller.AxisChanged += static (address, value) =>
{
    Console.WriteLine($"Axis {address} Changed: {value}");
};

controller.Start();

Console.ReadLine();

controller.Stop();
```

### Use loop

```csharp
using var controller = new GameController();

controller.Start();

while (true)
{
    Console.SetCursorPosition(0, 0);
    Console.WriteLine($"Connected: {controller.IsConnected.ToString(),-5}");
    for (var i = (byte)0; i < 8; i++)
    {
        Console.WriteLine($"Button {i}: {controller.GetButtonPressed(i).ToString(),-5}");
    }
    for (var i = (byte)0; i < 8; i++)
    {
        Console.WriteLine($"Axis {i}: {controller.GetAxisValue(i),6}");
    }

    Thread.Sleep(50);
}
```

# ⌨️LinuxDotNet.InputEvent

`/dev/input/event*` device reader.

## Usage

### List devices

```csharp
foreach (var device in EventDeviceInfo.GetDevices())
{
    Console.WriteLine($"{device.Device,-18}  {device.VendorId}:{device.ProductId}  {device.Name}");
}
```

### Barcode reader

- [BarcodeReader.cs](https://github.com/usausa/linux-dotnet/blob/main/Example.InputEvent.ConsoleApp/BarcodeReader.cs)

# 🖥️LinuxDotNet.SystemInfo

System information api.

## Usage

### Hardware

```csharp
var hw = PlatformProvider.GetHardware();

Console.WriteLine("[DMI]");
Console.WriteLine($"Vendor:         {hw.Vendor}");
Console.WriteLine($"ProductName:    {hw.ProductName}");

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
```

### Kernel

```csharp
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
```

### Uptime

```csharp
var uptime = PlatformProvider.GetUptime();
Console.WriteLine($"Uptime: {uptime.Uptime}");
```

### Stat

```csharp
var stat = PlatformProvider.GetSystemStat();
Console.WriteLine($"Interrupt:      {stat.Interrupt}");
Console.WriteLine($"ContextSwitch:  {stat.ContextSwitch}");
Console.WriteLine($"SoftIrq:        {stat.SoftIrq}");
Console.WriteLine($"ProcessRunning: {stat.ProcessRunning}");
Console.WriteLine($"ProcessBlocked: {stat.ProcessBlocked}");

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
```

### LoadAverage

```csharp
var load = PlatformProvider.GetLoadAverage();
Console.WriteLine($"Average1:  {load.Average1:F2}");
Console.WriteLine($"Average5:  {load.Average5:F2}");
Console.WriteLine($"Average15: {load.Average15:F2}");
```

### Memory

```csharp
var memory = PlatformProvider.GetMemoryStat();
Console.WriteLine($"MemoryTotal:     {memory.MemoryTotal}");
Console.WriteLine($"MemoryAvailable: {memory.MemoryAvailable}");
Console.WriteLine($"Buffers:         {memory.Buffers}");
Console.WriteLine($"Cached:          {memory.Cached}");
```

### VirtualMemory

```csharp
var vm = PlatformProvider.GetVirtualMemoryStat();
Console.WriteLine($"PageIn:            {vm.PageIn}");
Console.WriteLine($"PageOut:           {vm.PageOut}");
Console.WriteLine($"SwapIn:            {vm.SwapIn}");
Console.WriteLine($"SwapOut:           {vm.SwapOut}");
Console.WriteLine($"PageFault:         {vm.PageFault}");
Console.WriteLine($"MajorPageFault:    {vm.MajorPageFault}");
Console.WriteLine($"OutOfMemoryKiller: {vm.OutOfMemoryKiller}");
```

### Partition

```csharp
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
}
```

### Mount

```csharp
var mounts = PlatformProvider.GetMounts(IncludeVirtual);
foreach (var mount in mounts)
{
    Console.WriteLine($"Device:        {mount.DeviceName}");
    Console.WriteLine($"MountPoint:    {mount.MountPoint}");
    Console.WriteLine($"FileSystem:    {mount.FileSystem}");
    Console.WriteLine($"Options:       {mount.Option}");
    Console.WriteLine($"IsLocal:       {mount.IsLocal}");

    var usage = PlatformProvider.GetFileSystemUsage(mount.MountPoint);
    Console.WriteLine($"TotalSize:     {usage.TotalSize}");
    Console.WriteLine($"FreeSize:      {usage.FreeSize}");
    Console.WriteLine($"AvailableSize: {usage.AvailableSize}");
    Console.WriteLine($"TotalFiles:    {usage.TotalFiles}");
    Console.WriteLine($"FreeFiles:     {usage.FreeFiles}");
}
```

### DiskStat

```csharp
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
```

### NetworkStat

```csharp
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
```

### Tcp/Tcp6

```csharp
var tcp = PlatformProvider.GetTcpStat();
var tcp6 = PlatformProvider.GetTcp6Stat();
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
```

### ProcessSummary

```csharp
var process = PlatformProvider.GetProcessSummary();
Console.WriteLine($"ProcessCount: {process.ProcessCount}");
Console.WriteLine($"ThreadCount:  {process.ThreadCount}");
```

### Processes

```csharp
var processes = PlatformProvider.GetProcesses();

Console.WriteLine($"{"PID",-6} {"Name",-20} {"State",-12} {"User",-5} {"Threads",7} {"RSS (MB)",10} {"CPU Time",10}");
Console.WriteLine(new string('-', 76));

foreach (var p in processes)
{
    var rss = (double)p.ResidentSize / 1024 / 1024;
    var cpuTime = (p.UserTime + p.SystemTime) / 100.0;

    Console.WriteLine($"{p.ProcessId,-6} {TruncateName(p.Name, 20),-20} {p.State,-12} {p.UserId,-5} {p.ThreadCount,7} {rss,10:F2} {cpuTime,10:F2}");
}

static string TruncateName(string name, int maxLength) => name.Length <= maxLength ? name : name[..(maxLength - 3)] + "...";
```

### FileDescriptor

```csharp
var fd = PlatformProvider.GetFileHandleStat();

Console.WriteLine($"Allocated: {fd.Allocated}");
Console.WriteLine($"Used:      {fd.Used}");
Console.WriteLine($"Max:       {fd.Max}");
```

### Cpu

```csharp
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
```

### Mains

```csharp
var adapter = PlatformProvider.GetMainsDevice();
if (adapter.Supported)
{
    Console.WriteLine($"Online: {adapter.Online}");
}
else
{
    Console.WriteLine("No adapter found");
}
```

### Battery

```csharp
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
```

### HardwareMonitor

```csharp
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
```

## Link

* [Prometheus Exporter alternative](https://github.com/usausa/prometheus-exporter-alternative)

# 🎞️LinuxDotNet.Video4Linux2

Video for Linux API wrapper.

## Usage

### Video device information

```csharp
foreach (var device in VideoInfo.GetAllVideo())
{
    Console.WriteLine($"Device: {device.Device}");
    Console.WriteLine($"Available: {device.IsAvailable}");
    Console.WriteLine($"Name: {device.Name}");
    Console.WriteLine($"Driver: {device.Driver}");
    Console.WriteLine($"Bus: {device.BusInfo}");

    Console.WriteLine($"Capabilities: 0x{device.RawCapabilities:X8}");
    Console.WriteLine($"  Capture: {device.IsVideoCapture}");
    Console.WriteLine($"  Output: {device.IsVideoOutput}");
    Console.WriteLine($"  Metadata: {device.IsMetadata}");
    Console.WriteLine($"  Streaming: {device.IsStreaming}");

    Console.WriteLine($"Formats: {device.SupportedFormats.Count}");
    foreach (var format in device.SupportedFormats)
    {
        Console.WriteLine($"  Format: {format.PixelFormat}");
        Console.WriteLine($"    Description: {format.Description}");
        var resolutions = format.SupportedResolutions.Count > 0 ? $"{String.Join(", ", format.SupportedResolutions)}" : "(Nothing)";
        Console.WriteLine($"    Resolution: {resolutions}");
    }
}
```

### Snapshot

```csharp
using var capture = new VideoCapture(device);

var ret = capture.Open(width, height);
if (!ret)
{
    return;
}

width = capture.Width;
height = capture.Height;

// Snapshot
using var writer = new PooledBufferWriter<byte>(width * height * 2);
if (!capture.Snapshot(writer))
{
    Console.WriteLine("Snapshot failed.");
    return;
}

// Convert to RGBA
var buffer = new byte[width * height * 4];
ImageHelper.ConvertYUYV2RGBA(writer.WrittenSpan, buffer);

// Save
var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
using var image = SKImage.FromPixelCopy(info, buffer, width * 4);
using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
using var stream = File.OpenWrite(output);
data.SaveTo(stream);
```

### Capture

```csharp
using var capture = new VideoCapture(device);

var ret = capture.Open(width, height);
if (!ret)
{
    return;
}

capture.FrameCaptured += frame =>
{
    // Use frame data
};

capture.StartCapture();
```

## Image

![Video](https://github.com/usausa/linux-dotnet/blob/main/Document/video.png)
