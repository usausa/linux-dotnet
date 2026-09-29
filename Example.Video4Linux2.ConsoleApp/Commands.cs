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
        commands.AddCommand<ControlsCommand>();
        commands.AddCommand<CaptureCommand>();
        commands.AddCommand<SnapshotCommand>();
    }

    public static PixelFormat ParseFormat(string format) =>
        String.Equals(format, "mjpg", StringComparison.OrdinalIgnoreCase) ? PixelFormat.MJPG : PixelFormat.YUYV;
}

//--------------------------------------------------------------------------------
// Information
//--------------------------------------------------------------------------------
[Command("info", "Show information")]
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
            Console.WriteLine($"USB: {device.UsbPort} {device.UsbVendorId:x4}:{device.UsbProductId:x4}");
            Console.WriteLine($"ById: {device.ById}");
            Console.WriteLine($"ByPath: {device.ByPath}");

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
                foreach (var resolution in format.SupportedResolutions)
                {
                    Console.WriteLine($"      {resolution}: {String.Join(", ", format.GetFrameIntervals(resolution))} fps");
                }
            }

            Console.WriteLine($"Controls: {device.Controls.Count}");
            foreach (var control in device.Controls)
            {
                Console.WriteLine($"  {control.Name}: {control.Value} ({control.Type}, {control.Minimum}..{control.Maximum}, default {control.DefaultValue})");
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
// Controls
//--------------------------------------------------------------------------------
[Command("controls", "Show or change controls")]
public sealed class ControlsCommand : ICommandHandler
{
    [Option<string>("--device", "-d", Description = "Device", DefaultValue = "/dev/video0")]
    public string Device { get; set; } = default!;

    [Option<string>("--id", "-i", Description = "Control id (hex)", DefaultValue = "")]
    public string Id { get; set; } = default!;

    [Option<string>("--value", "-v", Description = "Value to set", DefaultValue = "")]
    public string Value { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var capture = new VideoCapture(Device);
        if (!capture.Open())
        {
            Console.WriteLine("Open failed.");
            return ValueTask.CompletedTask;
        }

        if ((Id.Length > 0) && (Value.Length > 0))
        {
            var id = Int32.Parse(Id.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
            var value = Int32.Parse(Value, System.Globalization.CultureInfo.InvariantCulture);
            Console.WriteLine($"Set: 0x{id:X8}={value} {capture.SetControl(id, value)} -> {capture.GetControl(id)}");
        }

        foreach (var control in capture.GetControls())
        {
            Console.WriteLine($"0x{control.Id:X8} {control.Name,-32} {control.Value,6} {control.Type,-10} {control.Minimum}..{control.Maximum} step {control.Step} default {control.DefaultValue} {control.Attributes}");
            foreach (var item in control.MenuItems)
            {
                Console.WriteLine($"             {item.Index}: {item}");
            }
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Capture
//--------------------------------------------------------------------------------
[Command("capture", "Capture video")]
public sealed class CaptureCommand : ICommandHandler
{
    [Option<string>("--device", "-d", Description = "Device", DefaultValue = "/dev/video0")]
    public string Device { get; set; } = default!;

    [Option<int>("--width", "-w", Description = "Width", DefaultValue = 640)]
    public int Width { get; set; }

    [Option<int>("--height", "-h", Description = "Height", DefaultValue = 480)]
    public int Height { get; set; }

    [Option<string>("--format", "-f", Description = "Format (yuyv or mjpg)", DefaultValue = "yuyv")]
    public string Format { get; set; } = default!;

    [Option<int>("--fps", Description = "Frame rate", DefaultValue = 0)]
    public int Fps { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var capture = new VideoCapture(Device);

        var ret = capture.Open(Width, Height, CommandBuilderExtensions.ParseFormat(Format));
        if (!ret)
        {
            Console.WriteLine("Open failed.");
            return ValueTask.CompletedTask;
        }

        if (Fps > 0)
        {
            capture.SetFrameRate(Fps);
        }

        Console.WriteLine($"Open: {ret} {capture.Width}x{capture.Height} {capture.PixelFormat} stride={capture.BytesPerLine} size={capture.ImageSize} fps={capture.FrameRate}");

        Console.CursorVisible = false;
        Console.Clear();

        var watch = Stopwatch.StartNew();
        var processed = 0;
        var dropped = 0;
        var errors = 0;
        var jpeg = 0;
        var lastSequence = (uint?)null;
        var firstTimestamp = TimeSpan.Zero;
        capture.FrameCaptured += frame =>
        {
            processed++;
            if (lastSequence is { } last)
            {
                dropped += (int)(frame.Sequence - last - 1);
            }
            else
            {
                firstTimestamp = frame.Timestamp;
            }

            lastSequence = frame.Sequence;
            errors += frame.IsError ? 1 : 0;
            jpeg += (frame.Length > 2) && (frame.Span[0] == 0xFF) && (frame.Span[1] == 0xD8) ? 1 : 0;

            var elapsed = watch.ElapsedMilliseconds;
            if (elapsed >= 1000)
            {
                var span = (frame.Timestamp - firstTimestamp).TotalSeconds;
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"FPS: {(double)processed / elapsed * 1000:F2} Sequence: {frame.Sequence} Dropped: {dropped} Errors: {errors} JPEG: {jpeg} Timestamp: {frame.Timestamp} Length: {frame.Length} Span: {span:F2}s");

                watch.Restart();
                processed = 0;
                jpeg = 0;
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
[Command("snapshot", "Snapshot image")]
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

    [Option<string>("--format", "-f", Description = "Format (yuyv or mjpg)", DefaultValue = "yuyv")]
    public string Format { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var capture = new VideoCapture(Device);

        var width = Width;
        var height = Height;
        var ret = capture.Open(width, height, CommandBuilderExtensions.ParseFormat(Format));
        if (!ret)
        {
            Console.WriteLine("Open failed.");
            return ValueTask.CompletedTask;
        }

        Console.WriteLine($"Open: {ret} {capture.Width}x{capture.Height} {capture.PixelFormat}");

        width = capture.Width;
        height = capture.Height;

        using var writer = new PooledBufferWriter<byte>(width * height * 2);
        if (!capture.Snapshot(writer))
        {
            Console.WriteLine("Snapshot failed.");
            return ValueTask.CompletedTask;
        }

        if (capture.PixelFormat == PixelFormat.MJPG)
        {
            File.WriteAllBytes(Output, writer.WrittenSpan);
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
