# Linux platform library for .NET

|Library|NuGet|
|:----|:----|
|LinuxDotNet.Cups|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Cups.svg)](https://www.nuget.org/packages/LinuxDotNet.Cups)|
|LinuxDotNet.Disk|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Disk.svg)](https://www.nuget.org/packages/LinuxDotNet.Disk)|
|LinuxDotNet.GameInput|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.GameInput.svg)](https://www.nuget.org/packages/LinuxDotNet.GameInput)|
|LinuxDotNet.InputEvent|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.InputEvent.svg)](https://www.nuget.org/packages/LinuxDotNet.InputEvent)|
|LinuxDotNet.SystemInfo|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.SystemInfo.svg)](https://www.nuget.org/packages/LinuxDotNet.SystemInfo)|
|LinuxDotNet.Video4Linux2|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Video4Linux2.svg)](https://www.nuget.org/packages/LinuxDotNet.Video4Linux2)|

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

### Uptime

```csharp
var uptime = PlatformProvider.GetUptime();
Console.WriteLine($"Uptime: {uptime.Uptime}");
```

### Statics

```csharp
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
var memory = PlatformProvider.GetMemory();
Console.WriteLine($"Total:   {memory.Total}");
Console.WriteLine($"Free:    {memory.Free}");
Console.WriteLine($"Buffers: {memory.Buffers}");
Console.WriteLine($"Cached:  {memory.Cached}");
Console.WriteLine($"Usage:   {(int)Math.Ceiling(memory.Usage)}");
```

### VirtualMemory

```csharp
var vm = PlatformProvider.GetVirtualMemory();
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
```

### DiskStatics

```csharp
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
```

### FileDescriptor

```csharp
var fd = PlatformProvider.GetFileDescriptor();
Console.WriteLine($"Allocated: {fd.Allocated}");
Console.WriteLine($"Used:      {fd.Used}");
Console.WriteLine($"Max:       {fd.Max}");
```

### NetworkStatic

```csharp
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
```

### Tcp/Tcp6

```csharp
var tcp = PlatformProvider.GetTcp();
var tcp6 = PlatformProvider.GetTcp6();
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

### Cpu

```csharp
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
```


### Battery

```csharp
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
```

### MainsAdapter

```csharp
var adapter = PlatformProvider.GetMainsAdapter();
if (adapter.Supported)
{
    Console.WriteLine($"Online: {adapter.Online}");
}
else
{
    Console.WriteLine("No adapter found");
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

![Windows](https://github.com/usausa/linux-dotnet/blob/main/Document/video.png)
