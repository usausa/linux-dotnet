#pragma warning disable CA1416
using Example.Video4Linux2;

using LinuxDotNet.Video4Linux2;

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
