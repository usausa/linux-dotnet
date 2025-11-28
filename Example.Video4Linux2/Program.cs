using LinuxDotNet.Video4Linux2;
using System;

for (var i = 0; i < 4; i++)
{
    var path = $"/dev/video{i}";
    if (!File.Exists(path))
    {
        break;
    }

    var device = CameraDevice.GetCameraInfo(path);

    Console.WriteLine("==== Device ====");

    Console.WriteLine($"デバイス: {device.Path}");
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
        Console.WriteLine($"  フォーマット: {format}");
        Console.WriteLine($"    ピクセルフォーマット: 0x{format.RawPixelFormat:X8} {format.FourCC} {format.PixelFormat}");
        Console.WriteLine("    解像度:");
        if (format.SupportedResolutions.Count > 0)
        {
            foreach (var resolution in format.SupportedResolutions)
            {
                Console.WriteLine($"      {resolution}");
            }
        }
        else
        {
            Console.WriteLine("      (解像度情報無し)");
        }
    }

    Console.WriteLine($"  キャプチャ適性: {CameraDeviceSelector.IsSuitableForCapture(device)}");
    Console.WriteLine($"  スコア: {CameraDeviceSelector.CalculateDeviceScore(device)}");
}
