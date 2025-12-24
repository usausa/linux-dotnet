namespace Example.Video4Linux2.ConsoleApp;

using System.Diagnostics;

using LinuxDotNet.Video4Linux2;

using SkiaSharp;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<InformationCommand>();
        commands.AddCommand<CaptureCommand>();
        commands.AddCommand<SnapshotCommand>();
    }
}

//--------------------------------------------------------------------------------
// Information
//--------------------------------------------------------------------------------
[Command("info", Description = "Show information")]
public sealed class InformationCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
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

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Capture
//--------------------------------------------------------------------------------
[Command("capture", Description = "Capture video")]
public sealed class CaptureCommand : ICommandHandler
{
    [Option<string>("--device", "-d", Description = "Device", DefaultValue = "/dev/video0")]
    public string Device { get; set; } = default!;

    [Option<int>("--width", "-w", Description = "Width", DefaultValue = 640)]
    public int Width { get; set; }

    [Option<int>("--height", "-h", Description = "Height", DefaultValue = 480)]
    public int Height { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var capture = new VideoCapture(Device);

        var ret = capture.Open(Width, Height);
        if (!ret)
        {
            return ValueTask.CompletedTask;
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

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Snapshot
//--------------------------------------------------------------------------------
[Command("snapshot", Description = "Snapshot image")]
public sealed class SnapshotCommand : ICommandHandler
{
    [Option<string>("--device", "-d", Description = "Device", DefaultValue = "/dev/video0")]
    public string Device { get; set; } = default!;

    [Option<string>("--output", "-o", Description = "Output filename", DefaultValue = "snapshot.jpg")]
    public string Output { get; set; } = default!;

    [Option<int>("--width", "-w", Description = "Width", DefaultValue = 640)]
    public int Width { get; set; }

    [Option<int>("--height", "-h", Description = "Height", DefaultValue = 480)]
    public int Height { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var capture = new VideoCapture(Device);

        var width = Width;
        var height = Height;
        var ret = capture.Open(width, height);
        if (!ret)
        {
            return ValueTask.CompletedTask;
        }

        Console.WriteLine($"Open: {ret} {capture.Width}x{capture.Height}");

        width = capture.Width;
        height = capture.Height;

        using var writer = new PooledBufferWriter<byte>(width * height * 2);
        if (!capture.Snapshot(writer))
        {
            Console.WriteLine("Snapshot failed.");
            return ValueTask.CompletedTask;
        }

        var buffer = new byte[width * height * 4];
        ImageHelper.ConvertYUYV2RGBA(writer.WrittenSpan, buffer);

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var image = SKImage.FromPixelCopy(info, buffer, width * 4);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        using var stream = File.OpenWrite(Output);
        data.SaveTo(stream);

        return ValueTask.CompletedTask;
    }
}
