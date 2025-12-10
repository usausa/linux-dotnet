// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
#pragma warning disable CA1416

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;

using Example.Video4Linux2;

using LinuxDotNet.Video4Linux2;

using SkiaSharp;

var rootCommand = new RootCommand("Camera example");

// Information
var infoCommand = new Command("info", "Show information");
infoCommand.Handler = CommandHandler.Create(static () =>
{
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

        Console.WriteLine("Helper");
        Console.WriteLine($"  Suitable: {VideoInfoHelper.IsSuitableForCapture(device)}");
        Console.WriteLine($"  Score: {VideoInfoHelper.CalculateDeviceScore(device)}");
        Console.WriteLine($"  Best: {VideoInfoHelper.SelectBestResolution(device.SupportedFormats)}");

        Console.WriteLine();
    }
});
rootCommand.Add(infoCommand);

// Capture
var captureCommand = new Command("capture", "Capture video");
captureCommand.AddOption(new Option<string>(["--device", "-d"], () => "/dev/video0", "Device"));
captureCommand.AddOption(new Option<int>(["--width", "-w"], () => 640, "Width"));
captureCommand.AddOption(new Option<int>(["--height", "-height"], () => 480, "Height"));
captureCommand.Handler = CommandHandler.Create(static (string device, int width, int height) =>
{
    using var capture = new VideoCapture(device);

    var ret = capture.Open(width, height);
    if (!ret)
    {
        return;
    }

    Console.WriteLine($"Open: {ret} {capture.Width}x{capture.Height}");

    Console.CursorVisible = false;
    Console.Clear();

    var watch = Stopwatch.StartNew();
    var processed = 0;
    capture.FrameCaptured += _ =>
    {
        processed++;

        var elapsed = watch.ElapsedMilliseconds;
        if (elapsed >= 1000)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"FPS: {(double)processed / elapsed * 1000:F2}");

            watch.Restart();
            processed = 0;
        }
    };

    capture.StartCapture();

    Console.ReadLine();

    capture.StopCapture();

    Console.CursorVisible = true;
});
rootCommand.Add(captureCommand);

// Snapshot
var snapshotCommand = new Command("snapshot", "Snapshot image");
snapshotCommand.AddOption(new Option<string>(["--device", "-d"], () => "/dev/video0", "Device"));
snapshotCommand.AddOption(new Option<string>(["--output", "-o"], () => "snapshot.jpg", "Output filename"));
snapshotCommand.AddOption(new Option<int>(["--width", "-w"], () => 640, "Width"));
snapshotCommand.AddOption(new Option<int>(["--height", "-h"], () => 480, "Height"));
snapshotCommand.Handler = CommandHandler.Create(static (string device, string output, int width, int height) =>
{
    using var capture = new VideoCapture(device);

    var ret = capture.Open(width, height);
    if (!ret)
    {
        return;
    }

    Console.WriteLine($"Open: {ret} {capture.Width}x{capture.Height}");

    width = capture.Width;
    height = capture.Height;

    using var writer = new PooledBufferWriter<byte>(width * height * 2);
    if (!capture.Snapshot(writer))
    {
        Console.WriteLine("Snapshot failed.");
        return;
    }

    var buffer = new byte[width * height * 4];
    ImageHelper.ConvertYUYV2RGBA(writer.WrittenSpan, buffer);

    var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
    using var image = SKImage.FromPixelCopy(info, buffer, width * 4);
    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
    using var stream = File.OpenWrite(output);
    data.SaveTo(stream);
});
rootCommand.Add(snapshotCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
