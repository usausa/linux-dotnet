namespace LinuxDotNet.Video4Linux2;

using System.Runtime.Versioning;
using System.Text;

using static LinuxDotNet.Video4Linux2.NativeMethods;

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

// ReSharper disable InconsistentNaming
#pragma warning disable CA1008
public enum PixelFormat
{
    YUYV = 0x56595559, // 'YUYV'
    MJPG = 0x47504a4d  // 'MJPG'
}
#pragma warning restore CA1008
// ReSharper restore InconsistentNaming

public sealed class VideoFormat
{
    public PixelFormat PixelFormat { get; }

    public string Description { get; }

    public IReadOnlyList<Resolution> SupportedResolutions { get; }

    internal VideoFormat(uint pixelFormat, string description, IReadOnlyList<Resolution> supportedResolutions)
    {
        PixelFormat = (PixelFormat)pixelFormat;
        Description = description;
        SupportedResolutions = supportedResolutions;
    }

    public override string ToString() => $"{Description} ({PixelFormat})";
}

[SupportedOSPlatform("linux")]
public sealed class VideoInfo
{
    public string Device { get; }

    public string Name { get; }

    public string Driver { get; }

    public string BusInfo { get; }

    public bool IsAvailable { get; }

    public uint RawCapabilities { get; }

    public IReadOnlyList<VideoFormat> SupportedFormats { get; }

    internal VideoInfo(string device, string name, string driver, string busInfo, bool isAvailable, uint capabilities, IReadOnlyList<VideoFormat> supportedFormats)
    {
        Device = device;
        Name = name;
        Driver = driver;
        BusInfo = busInfo;
        IsAvailable = isAvailable;
        RawCapabilities = capabilities;
        SupportedFormats = supportedFormats;
    }

    public override string ToString() => $"{Name} ({Device})";

    public static unsafe VideoInfo GetVideoInfo(string path)
    {
        var fd = open(path, O_RDWR);
        if (fd < 0)
        {
            throw new FileNotFoundException($"Failed to open device. path=[{path}]");
        }

        try
        {
            v4l2_capability capability;

            if (ioctl(fd, VIDIOC_QUERYCAP, (IntPtr)(&capability)) < 0)
            {
                return new VideoInfo(path, "Unknown", string.Empty, string.Empty, false, 0, []);
            }

            var isVideoCapture = (capability.capabilities & V4L2_CAP_VIDEO_CAPTURE) != 0;
            return new VideoInfo(
                path,
                Encoding.ASCII.GetString(capability.card, v4l2_capability.CardSize).TrimEnd('\0'),
                Encoding.ASCII.GetString(capability.driver, v4l2_capability.DriverSize).TrimEnd('\0'),
                Encoding.ASCII.GetString(capability.bus_info, v4l2_capability.BusInfoSize).TrimEnd('\0'),
                isVideoCapture,
                capability.capabilities,
                GetSupportedFormats(fd));
        }
        finally
        {
            _ = close(fd);
        }
    }

    public static IEnumerable<VideoInfo> GetAllVideo()
    {
        const string sysfsPath = "/sys/class/video4linux";

        if (!Directory.Exists(sysfsPath))
        {
            yield break;
        }

        foreach (var name in Directory.GetDirectories(sysfsPath).Select(Path.GetFileName).Where(static x => x?.StartsWith("video", StringComparison.Ordinal) ?? false).OrderBy(static x => x))
        {
            yield return GetVideoInfo($"/dev/{name}");
        }
    }

    private static unsafe List<VideoFormat> GetSupportedFormats(int fd)
    {
        var formats = new List<VideoFormat>();

        var index = 0u;
        while (true)
        {
            v4l2_fmtdesc formatDesc;
            formatDesc.index = index;
            formatDesc.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;

            if (ioctl(fd, VIDIOC_ENUM_FMT, (IntPtr)(&formatDesc)) < 0)
            {
                break;
            }

            var description = Encoding.ASCII.GetString(formatDesc.description, v4l2_fmtdesc.DescriptionSize).TrimEnd('\0');
            var format = new VideoFormat(formatDesc.pixelformat, description, GetSupportedResolutions(fd, formatDesc.pixelformat));

            formats.Add(format);
            index++;
        }

        return formats.OrderBy(static x => x.PixelFormat).ToList();
    }

    private static unsafe List<Resolution> GetSupportedResolutions(int fd, uint pixelFormat)
    {
        var resolutions = new List<Resolution>();

        var index = 0u;
        while (true)
        {
            v4l2_frmsizeenum frmSize;
            frmSize.index = index;
            frmSize.pixel_format = pixelFormat;

            if (ioctl(fd, VIDIOC_ENUM_FRAMESIZES, (IntPtr)(&frmSize)) < 0)
            {
                break;
            }

            if (frmSize.type == V4L2_FRMSIZE_TYPE_DISCRETE)
            {
                resolutions.Add(new Resolution((int)frmSize.size.discrete.width, (int)frmSize.size.discrete.height));
            }
            else if ((frmSize.type == V4L2_FRMSIZE_TYPE_STEPWISE) || (frmSize.type == V4L2_FRMSIZE_TYPE_CONTINUOUS))
            {
                AddCommonResolutions(resolutions, frmSize.size.stepwise);
                break;
            }

            index++;
        }

        return resolutions.OrderBy(static x => x.Width).ThenBy(static x => x.Height).ToList();
    }

    private static void AddCommonResolutions(List<Resolution> resolutions, v4l2_frmsize_stepwise stepwise)
    {
        // ReSharper disable CommentTypo
        var commonResolutions = new[]
        {
            new Resolution(160, 120),   // QQVGA
            new Resolution(320, 240),   // QVGA
            new Resolution(640, 480),   // VGA
            new Resolution(800, 600),   // SVGA
            new Resolution(1024, 768),  // XGA
            new Resolution(1280, 720),  // HD
            new Resolution(1280, 960),
            new Resolution(1920, 1080), // Full HD
            new Resolution(2560, 1440), // 2K
            new Resolution(3840, 2160)  // 4K
        };
        // ReSharper restore CommentTypo

        foreach (var res in commonResolutions)
        {
            if ((res.Width >= stepwise.min_width) && (res.Width <= stepwise.max_width) && (res.Height >= stepwise.min_height) && (res.Height <= stepwise.max_height))
            {
                resolutions.Add(res);
            }
        }
    }
}

[SupportedOSPlatform("linux")]
public static class Extensions
{
    extension(VideoInfo info)
    {
        public bool IsVideoCapture => (info.RawCapabilities & V4L2_CAP_VIDEO_CAPTURE) != 0;

        public bool IsVideoOutput => (info.RawCapabilities & V4L2_CAP_VIDEO_OUTPUT) != 0;

        public bool IsMetadata => (info.RawCapabilities & V4L2_CAP_META_CAPTURE) != 0;

        public bool IsStreaming => (info.RawCapabilities & V4L2_CAP_STREAMING) != 0;
    }
}
