namespace LinuxDotNet.Video4Linux2;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

using static LinuxDotNet.Video4Linux2.NativeMethods;

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

// TODO split info and control
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

    public static VideoInfo GetVideoInfo(string path)
    {
        var fd = open(path, O_RDWR);
        if (fd < 0)
        {
            throw new FileNotFoundException($"Failed to open device. path=[{path}]");
        }

        try
        {
            var cap = default(v4l2_capability);

            var capPtr = Marshal.AllocHGlobal(Marshal.SizeOf(cap));
            try
            {
                Marshal.StructureToPtr(cap, capPtr, false);
                if (ioctl(fd, VIDIOC_QUERYCAP, capPtr) < 0)
                {
                    return new VideoInfo(path, "Unknown", string.Empty, string.Empty, false, 0, []);
                }

                cap = Marshal.PtrToStructure<v4l2_capability>(capPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(capPtr);
            }

            var isVideoCapture = (cap.capabilities & V4L2_CAP_VIDEO_CAPTURE) != 0;

            var camera = new VideoInfo(
                path,
                Encoding.ASCII.GetString(cap.card).TrimEnd('\0'),
                Encoding.ASCII.GetString(cap.driver).TrimEnd('\0'),
                Encoding.ASCII.GetString(cap.bus_info).TrimEnd('\0'),
                isVideoCapture,
                cap.capabilities,
                VideoDeviceHelper.GetSupportedFormats(fd));

            return camera;
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
