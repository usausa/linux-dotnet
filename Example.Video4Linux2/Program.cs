using LinuxDotNet.Video4Linux2;

for (var i = 0; i < 4; i++)
{
    var device = $"/dev/video{i}";
    if (!File.Exists(device))
    {
        break;
    }

    Console.WriteLine($"==== Device: {device} ====");

    var formats = CameraDeviceHelper.GetSupportedFormats(device);
    foreach (var format in formats)
    {
        Console.WriteLine($"  フォーマット: {format}");
        Console.WriteLine($"  ピクセルフォーマット: 0x{format.PixelFormat:X8}");
        Console.WriteLine("  対応解像度:");
        if (format.SupportedResolutions.Count > 0)
        {
            foreach (var resolution in format.SupportedResolutions)
            {
                Console.WriteLine($"    {resolution}");
            }
        }
        else
        {
            Console.WriteLine("    (解像度情報無し)");
        }
    }

    Console.WriteLine();
}
