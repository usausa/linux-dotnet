namespace LinuxDotNet.Video4Linux2;

using System.Runtime.InteropServices;
using System.Text;

public readonly struct Resolution : IEquatable<Resolution>
{
    public int Width { get; }

    public int Height { get; }

    internal Resolution(int width, int height)
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

public enum PixelFormatType
{
    Unknown = 0,
    Yuyv,
    MotionJpeg
}

public sealed class VideoFormat
{
    public PixelFormatType PixelFormat { get; }

    public uint RawPixelFormat { get; }

    public string Description { get; }

    // ReSharper disable once InconsistentNaming
    public string FourCC { get; }

    public IReadOnlyList<Resolution> SupportedResolutions { get; }

    internal VideoFormat(uint pixelFormat, string description, IReadOnlyList<Resolution> supportedResolutions)
    {
        PixelFormat = pixelFormat switch
        {
            0x56595559 => PixelFormatType.Yuyv,
            0x47504A4D => PixelFormatType.MotionJpeg,
            _ => PixelFormatType.Unknown
        };
        RawPixelFormat = pixelFormat;
        Description = description;
        FourCC = new string([(char)(pixelFormat & 0xFF), (char)((pixelFormat >> 8) & 0xFF), (char)((pixelFormat >> 16) & 0xFF), (char)((pixelFormat >> 24) & 0xFF)]);
        SupportedResolutions = supportedResolutions;
    }

    public override string ToString() => $"{Description} ({FourCC})";
}

// TODO split info and control
public sealed class CameraDevice
{
    public string Path { get; }

    public string Name { get; }

    public string Driver { get; }

    public string BusInfo { get; }

    public bool IsAvailable { get; }

    public uint RawCapabilities { get; }

    public IReadOnlyList<VideoFormat> SupportedFormats { get; }

    public bool IsVideoCapture => (RawCapabilities & NativeMethods.V4L2_CAP_VIDEO_CAPTURE) != 0;

    public bool IsVideoOutput => (RawCapabilities & NativeMethods.V4L2_CAP_VIDEO_OUTPUT) != 0;

    public bool IsMetadata => (RawCapabilities & NativeMethods.V4L2_CAP_META_CAPTURE) != 0;

    public bool IsStreaming => (RawCapabilities & NativeMethods.V4L2_CAP_STREAMING) != 0;

    internal CameraDevice(string path, string name, string driver, string busInfo, bool isAvailable, uint capabilities, IReadOnlyList<VideoFormat> supportedFormats)
    {
        Path = path;
        Name = name;
        Driver = driver;
        BusInfo = busInfo;
        IsAvailable = isAvailable;
        RawCapabilities = capabilities;
        SupportedFormats = supportedFormats;
    }

    public override string ToString() => $"{Name} ({Path})";

    public static CameraDevice GetCameraInfo(string path)
    {
        var fd = NativeMethods.open(path, NativeMethods.O_RDWR);
        if (fd < 0)
        {
            throw new FileNotFoundException($"Failed to open device. path=[{path}]");
        }

        try
        {
            var cap = default(NativeMethods.v4l2_capability);
            var capPtr = Marshal.AllocHGlobal(Marshal.SizeOf(cap));
            Marshal.StructureToPtr(cap, capPtr, false);

            if (NativeMethods.ioctl(fd, NativeMethods.VIDIOC_QUERYCAP, capPtr) < 0)
            {
                Marshal.FreeHGlobal(capPtr);
                return new CameraDevice(path, "Unknown", string.Empty, string.Empty, false, 0, []);
            }

            cap = Marshal.PtrToStructure<NativeMethods.v4l2_capability>(capPtr);
            Marshal.FreeHGlobal(capPtr);

            var isVideoCapture = (cap.capabilities & NativeMethods.V4L2_CAP_VIDEO_CAPTURE) != 0;
            var isMetadata = (cap.capabilities & NativeMethods.V4L2_CAP_META_CAPTURE) != 0;

            // TODO twice open
            var camera = new CameraDevice(
                path,
                Encoding.ASCII.GetString(cap.card).TrimEnd('\0'),
                Encoding.ASCII.GetString(cap.driver).TrimEnd('\0'),
                Encoding.ASCII.GetString(cap.bus_info).TrimEnd('\0'),
                isVideoCapture,
                cap.capabilities,
                isVideoCapture && !isMetadata ? CameraDeviceHelper.GetSupportedFormats(path) : []);

            return camera;
        }
        finally
        {
            _ = NativeMethods.close(fd);
        }
    }
}
