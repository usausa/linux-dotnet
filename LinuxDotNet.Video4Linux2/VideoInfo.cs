namespace LinuxDotNet.Video4Linux2;

using System.Globalization;
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

    public override bool Equals(object? obj) => (obj is Resolution other) && Equals(other);

    public bool Equals(Resolution other) => (Width == other.Width) && (Height == other.Height);

    public static bool operator ==(Resolution x, Resolution y) => x.Equals(y);

    public static bool operator !=(Resolution x, Resolution y) => !x.Equals(y);

    public override string ToString() => $"{Width}x{Height}";
}

public readonly struct FrameInterval : IEquatable<FrameInterval>
{
    public uint Numerator { get; }

    public uint Denominator { get; }

    public double FramesPerSecond => Numerator == 0 ? 0 : (double)Denominator / Numerator;

    public FrameInterval(uint numerator, uint denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public override bool Equals(object? obj) => (obj is FrameInterval other) && Equals(other);

    public bool Equals(FrameInterval other) => (Numerator == other.Numerator) && (Denominator == other.Denominator);

    public static bool operator ==(FrameInterval x, FrameInterval y) => x.Equals(y);

    public static bool operator !=(FrameInterval x, FrameInterval y) => !x.Equals(y);

    public override string ToString() => FramesPerSecond.ToString("0.##", CultureInfo.InvariantCulture);
}

// ReSharper disable InconsistentNaming
#pragma warning disable CA1008
public enum PixelFormat
{
    YUYV = 0x56595559, // 'YUYV'
    MJPG = 0x47504a4d, // 'MJPG'
    UYVY = 0x59565955, // 'UYVY'
    NV12 = 0x3231564e, // 'NV12'
    YU12 = 0x32315559, // 'YU12'
    RGB3 = 0x33424752, // 'RGB3'
    BGR3 = 0x33524742, // 'BGR3'
    GREY = 0x59455247, // 'GREY'
    H264 = 0x34363248  // 'H264'
}
#pragma warning restore CA1008
// ReSharper restore InconsistentNaming

public sealed class VideoFormat
{
    private readonly IReadOnlyDictionary<Resolution, IReadOnlyList<FrameInterval>> frameIntervals;

    public PixelFormat PixelFormat { get; }

    public string Description { get; }

    public IReadOnlyList<Resolution> SupportedResolutions { get; }

    internal VideoFormat(uint pixelFormat, string description, IReadOnlyList<Resolution> supportedResolutions, IReadOnlyDictionary<Resolution, IReadOnlyList<FrameInterval>> frameIntervals)
    {
        PixelFormat = (PixelFormat)pixelFormat;
        Description = description;
        SupportedResolutions = supportedResolutions;
        this.frameIntervals = frameIntervals;
    }

    public IReadOnlyList<FrameInterval> GetFrameIntervals(Resolution resolution) =>
        frameIntervals.TryGetValue(resolution, out var intervals) ? intervals : [];

    public override string ToString() => $"{Description} ({PixelFormat})";
}

public sealed class VideoInfo
{
    private const string SysfsPath = "/sys/class/video4linux";

    private const string ByIdPath = "/dev/v4l/by-id";

    private const string ByPathPath = "/dev/v4l/by-path";

    private const int UsbSearchDepth = 4;

    public string Device { get; }

    public string Name { get; }

    public string Driver { get; }

    public string BusInfo { get; }

    public bool IsAvailable { get; }

    public uint RawCapabilities { get; }

    public IReadOnlyList<VideoFormat> SupportedFormats { get; }

    public IReadOnlyList<VideoControl> Controls { get; }

    public string UsbPort { get; }

    public ushort UsbVendorId { get; }

    public ushort UsbProductId { get; }

    public string ById { get; }

    public string ByPath { get; }

    internal VideoInfo(string device, string name, string driver, string busInfo, bool isAvailable, uint capabilities, IReadOnlyList<VideoFormat> supportedFormats, IReadOnlyList<VideoControl> controls)
    {
        Device = device;
        Name = name;
        Driver = driver;
        BusInfo = busInfo;
        IsAvailable = isAvailable;
        RawCapabilities = capabilities;
        SupportedFormats = supportedFormats;
        Controls = controls;

        var real = ResolveDevice(device);
        (UsbPort, UsbVendorId, UsbProductId) = ReadUsbInfo(real);
        ById = FindLink(ByIdPath, real);
        ByPath = FindLink(ByPathPath, real);
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
                return new VideoInfo(path, "Unknown", string.Empty, string.Empty, false, 0, [], []);
            }

            var isVideoCapture = (capability.capabilities & V4L2_CAP_VIDEO_CAPTURE) != 0;
            return new VideoInfo(
                path,
                Encoding.ASCII.GetString(capability.card, v4l2_capability.CardSize).TrimEnd('\0'),
                Encoding.ASCII.GetString(capability.driver, v4l2_capability.DriverSize).TrimEnd('\0'),
                Encoding.ASCII.GetString(capability.bus_info, v4l2_capability.BusInfoSize).TrimEnd('\0'),
                isVideoCapture,
                capability.capabilities,
                GetSupportedFormats(fd),
                isVideoCapture ? VideoControl.Query(fd) : []);
        }
        finally
        {
            _ = close(fd);
        }
    }

    public static IEnumerable<VideoInfo> GetAllVideo()
    {
        if (!Directory.Exists(SysfsPath))
        {
            yield break;
        }

        foreach (var name in Directory.GetDirectories(SysfsPath).Select(Path.GetFileName).Where(static x => x?.StartsWith("video", StringComparison.Ordinal) ?? false).OrderBy(static x => Int32.TryParse(x.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : Int32.MaxValue))
        {
            VideoInfo info;
            try
            {
                info = GetVideoInfo($"/dev/{name}");
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            yield return info;
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
            var pixelFormat = formatDesc.pixelformat;
            var resolutions = GetSupportedResolutions(fd, pixelFormat);
            var intervals = resolutions.ToDictionary(static x => x, x => (IReadOnlyList<FrameInterval>)GetFrameIntervals(fd, pixelFormat, x));
            var format = new VideoFormat(pixelFormat, description, resolutions, intervals);

            formats.Add(format);
            index++;
        }

#pragma warning disable IDE0028
        return formats.OrderBy(static x => x.PixelFormat).ToList();
#pragma warning restore IDE0028
    }

    private static List<Resolution> GetSupportedResolutions(int fd, uint pixelFormat)
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

#pragma warning disable IDE0028
        return resolutions.OrderBy(static x => x.Width).ThenBy(static x => x.Height).ToList();
#pragma warning restore IDE0028
    }

    private static List<FrameInterval> GetFrameIntervals(int fd, uint pixelFormat, Resolution resolution)
    {
        var intervals = new List<FrameInterval>();

        var index = 0u;
        while (true)
        {
            var frmInterval = default(v4l2_frmivalenum);
            frmInterval.index = index;
            frmInterval.pixel_format = pixelFormat;
            frmInterval.width = (uint)resolution.Width;
            frmInterval.height = (uint)resolution.Height;

            if (ioctl(fd, VIDIOC_ENUM_FRAMEINTERVALS, (IntPtr)(&frmInterval)) < 0)
            {
                break;
            }

            if (frmInterval.type == V4L2_FRMIVAL_TYPE_DISCRETE)
            {
                intervals.Add(new FrameInterval(frmInterval.interval.discrete.numerator, frmInterval.interval.discrete.denominator));
            }
            else if ((frmInterval.type == V4L2_FRMIVAL_TYPE_STEPWISE) || (frmInterval.type == V4L2_FRMIVAL_TYPE_CONTINUOUS))
            {
                intervals.Add(new FrameInterval(frmInterval.interval.stepwise.min.numerator, frmInterval.interval.stepwise.min.denominator));
                intervals.Add(new FrameInterval(frmInterval.interval.stepwise.max.numerator, frmInterval.interval.stepwise.max.denominator));
                break;
            }

            index++;
        }

#pragma warning disable IDE0028
        return intervals.Distinct().OrderByDescending(static x => x.FramesPerSecond).ToList();
#pragma warning restore IDE0028
    }

    private static string ResolveDevice(string device)
    {
        try
        {
            return new FileInfo(device).ResolveLinkTarget(true)?.FullName ?? device;
        }
        catch (IOException)
        {
            return device;
        }
    }

    private static (string Port, ushort VendorId, ushort ProductId) ReadUsbInfo(string device)
    {
        try
        {
            var entry = new DirectoryInfo(Path.Combine(SysfsPath, Path.GetFileName(device)));
            var current = (entry.ResolveLinkTarget(true) as DirectoryInfo)?.Parent;
            for (var depth = 0; (current is not null) && (depth < UsbSearchDepth); depth++)
            {
                if (TryReadHex(Path.Combine(current.FullName, "idVendor"), out var vendorId) &&
                    TryReadHex(Path.Combine(current.FullName, "idProduct"), out var productId))
                {
                    return (current.Name, vendorId, productId);
                }

                current = current.Parent;
            }

            return (string.Empty, 0, 0);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (string.Empty, 0, 0);
        }
    }

    private static bool TryReadHex(string path, out ushort value)
    {
        value = 0;
        try
        {
            return File.Exists(path) && UInt16.TryParse(File.ReadAllText(path).AsSpan().Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string FindLink(string directory, string device)
    {
        if (!Directory.Exists(directory))
        {
            return string.Empty;
        }

        try
        {
            foreach (var file in Directory.GetFiles(directory).Order(StringComparer.Ordinal))
            {
                if (new FileInfo(file).ResolveLinkTarget(true)?.FullName == device)
                {
                    return file;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }

        return string.Empty;
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

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var res in commonResolutions)
        {
            if ((res.Width >= stepwise.min_width) && (res.Width <= stepwise.max_width) && (res.Height >= stepwise.min_height) && (res.Height <= stepwise.max_height))
            {
                resolutions.Add(res);
            }
        }
    }
}

#pragma warning disable CA1034
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
#pragma warning restore CA1034
