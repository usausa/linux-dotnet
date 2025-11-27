namespace LinuxDotNet.Video4Linux2;

public readonly struct Resolution : IEquatable<Resolution>
{
    public int Width { get; }

    public int Height { get; }

    public Resolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public override int GetHashCode() => HashCode.Combine(Width, Height);

    public override bool Equals(object? obj) => obj is Resolution other && Equals(other);

    public bool Equals(Resolution other) => Width == other.Width && Height == other.Height;

    public static bool operator ==(Resolution x, Resolution y) => x.Equals(y);

    public static bool operator !=(Resolution x, Resolution y) => !x.Equals(y);

    public override string ToString() => $"{Width}x{Height}";
}

public sealed class VideoFormat
{
    public uint PixelFormat { get; }

    public string Description { get; }

    // ReSharper disable once InconsistentNaming
    public string FourCC { get; }

    public IReadOnlyList<Resolution> SupportedResolutions { get; }

    public VideoFormat(uint pixelFormat, string description, IReadOnlyList<Resolution> supportedResolutions)
    {
        PixelFormat = pixelFormat;
        Description = description;
        FourCC = new string([(char)(pixelFormat & 0xFF), (char)((pixelFormat >> 8) & 0xFF), (char)((pixelFormat >> 16) & 0xFF), (char)((pixelFormat >> 24) & 0xFF)]);
        SupportedResolutions = supportedResolutions;
    }

    public override string ToString() => $"{Description} ({FourCC})";
}

public sealed class CameraDevice
{
    public string DevicePath { get; }

    public string Name { get; }

    public string Driver { get; }

    public string BusInfo { get; }

    public bool IsAvailable { get; }

    public uint Capabilities { get; }

    public IReadOnlyList<VideoFormat> SupportedFormats { get; }

    public bool IsVideoCapture => (Capabilities & NativeMethods.V4L2_CAP_VIDEO_CAPTURE) != 0;

    public bool IsMetadata => (Capabilities & NativeMethods.V4L2_CAP_META_CAPTURE) != 0;

    public bool IsOutputDevice => (Capabilities & NativeMethods.V4L2_CAP_VIDEO_OUTPUT) != 0;

    public CameraDevice(string devicePath, string name, string driver, string busInfo, bool isAvailable, uint capabilities, IReadOnlyList<VideoFormat> supportedFormats)
    {
        DevicePath = devicePath;
        Name = name;
        Driver = driver;
        BusInfo = busInfo;
        IsAvailable = isAvailable;
        Capabilities = capabilities;
        SupportedFormats = supportedFormats;
    }

    public override string ToString() => $"{Name} ({DevicePath})";
}
