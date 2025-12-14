# Linux library wrapper for .NET

|Library|NuGet|
|:----|:----|
|LinuxDotNet.Cups|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Cups.svg)](https://www.nuget.org/packages/LinuxDotNet.Cups)|
|LinuxDotNet.GameInput|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.GameInput.svg)](https://www.nuget.org/packages/LinuxDotNet.GameInput)|
|LinuxDotNet.Video4Linux2|[![NuGet](https://img.shields.io/nuget/v/LinuxDotNet.Video4Linux2.svg)](https://www.nuget.org/packages/LinuxDotNet.Video4Linux2)|

# LinuxDotNet.Cups

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

# LinuxDotNet.GameInput

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

# LinuxDotNet.Video4Linux2

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
