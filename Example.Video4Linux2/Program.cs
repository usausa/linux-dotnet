#pragma warning disable CA1416
using Example.Video4Linux2;

using LinuxDotNet.Video4Linux2;

foreach (var device in VideoInfo.GetAllVideo())
{
    Console.WriteLine($"デバイス: {device.Device}");
    Console.WriteLine($"利用可能: {device.IsAvailable}");
    Console.WriteLine($"名前: {device.Name}");
    Console.WriteLine($"ドライバー: {device.Driver}");
    Console.WriteLine($"バス: {device.BusInfo}");

    Console.WriteLine($"Capabilities: 0x{device.RawCapabilities:X8}");
    Console.WriteLine($"  キャプチャ: {device.IsVideoCapture}");
    Console.WriteLine($"  出力: {device.IsVideoOutput}");
    Console.WriteLine($"  メタデータ: {device.IsMetadata}");
    Console.WriteLine($"  ストリーミング: {device.IsStreaming}");

    Console.WriteLine($"フォーマット数: {device.SupportedFormats.Count}");
    foreach (var format in device.SupportedFormats)
    {
        Console.WriteLine($"  フォーマット: {format.PixelFormat}");
        Console.WriteLine($"    詳細: {format.Description}");
        var resolutions = format.SupportedResolutions.Count > 0 ? $"{String.Join(", ", format.SupportedResolutions)}" : "(解像度情報無し)";
        Console.WriteLine($"    解像度: {resolutions}");
    }

    Console.WriteLine($"キャプチャ適性: {VideoInfoSelector.IsSuitableForCapture(device)}");
    Console.WriteLine($"スコア: {VideoInfoSelector.CalculateDeviceScore(device)}");

    Console.WriteLine();
}
