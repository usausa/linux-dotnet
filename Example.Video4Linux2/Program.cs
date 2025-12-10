// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
#pragma warning disable CA1416

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

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

        Console.WriteLine($"Suitable: {VideoInfoSelector.IsSuitableForCapture(device)}");
        Console.WriteLine($"Score: {VideoInfoSelector.CalculateDeviceScore(device)}");

        Console.WriteLine();
    }
});
rootCommand.Add(infoCommand);

// Capture
var captureCommand = new Command("capture", "Capture video");
captureCommand.Handler = CommandHandler.Create(static () =>
{
    using var capture = new VideoCapture("/dev/video0");

    var ret = capture.Open();
    Console.WriteLine($"Open: {ret} {capture.Width}x{capture.Height}");

    if (!ret)
    {
        return;
    }

    var width = capture.Width;
    var height = capture.Height;
    var processed = false;
    capture.FrameCaptured += x =>
    {
        if (!processed)
        {
            var buffer = new byte[width * height * 4];
            ImageHelper.ConvertYUYV2RGBA(x.AsSpan(), buffer);

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var image = SKImage.FromPixelCopy(info, buffer, width * 4);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            using var stream = File.OpenWrite("output.jpg");
            data.SaveTo(stream);

            processed = true;

            Console.WriteLine("Captured.");
        }
    };

    capture.StartCapture();

    Console.ReadLine();

    capture.StopCapture();
});
rootCommand.Add(captureCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
