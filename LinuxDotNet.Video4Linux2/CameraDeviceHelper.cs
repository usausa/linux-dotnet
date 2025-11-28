namespace LinuxDotNet.Video4Linux2;

using System.Runtime.InteropServices;
using System.Text;

// TODO internal
public static class CameraDeviceHelper
{
    public static IReadOnlyList<VideoFormat> GetSupportedFormats(string path)
    {
        var fd = NativeMethods.open(path, NativeMethods.O_RDWR);
        if (fd < 0)
        {
            throw new FileNotFoundException($"Failed to open device. path=[{path}]");
        }

        var formats = new List<VideoFormat>();

        try
        {
            uint index = 0;
            while (true)
            {
                var fmtDesc = new NativeMethods.v4l2_fmtdesc
                {
                    index = index,
                    type = NativeMethods.V4L2_BUF_TYPE_VIDEO_CAPTURE
                };
                var fmtDescPtr = Marshal.AllocHGlobal(Marshal.SizeOf(fmtDesc));
                Marshal.StructureToPtr(fmtDesc, fmtDescPtr, false);

                if (NativeMethods.ioctl(fd, NativeMethods.VIDIOC_ENUM_FMT, fmtDescPtr) < 0)
                {
                    Marshal.FreeHGlobal(fmtDescPtr);
                    break;
                }

                fmtDesc = Marshal.PtrToStructure<NativeMethods.v4l2_fmtdesc>(fmtDescPtr);
                Marshal.FreeHGlobal(fmtDescPtr);

                var format = new VideoFormat(
                    fmtDesc.pixelformat,
                    Encoding.ASCII.GetString(fmtDesc.description).TrimEnd('\0'),
                    GetSupportedResolutions(fd, fmtDesc.pixelformat));

                formats.Add(format);
                index++;
            }
        }
        finally
        {
            _ = NativeMethods.close(fd);
        }

        return formats.OrderBy(static x => x.PixelFormat).ToList();
    }

    private static List<Resolution> GetSupportedResolutions(int fd, uint pixelFormat)
    {
        var resolutions = new List<Resolution>();
        uint index = 0;

        while (true)
        {
            var frmSize = new NativeMethods.v4l2_frmsizeenum
            {
                index = index,
                pixel_format = pixelFormat
            };
            var frmSizePtr = Marshal.AllocHGlobal(Marshal.SizeOf(frmSize));
            Marshal.StructureToPtr(frmSize, frmSizePtr, false);

            if (NativeMethods.ioctl(fd, NativeMethods.VIDIOC_ENUM_FRAMESIZES, frmSizePtr) < 0)
            {
                Marshal.FreeHGlobal(frmSizePtr);
                break;
            }

            frmSize = Marshal.PtrToStructure<NativeMethods.v4l2_frmsizeenum>(frmSizePtr);
            Marshal.FreeHGlobal(frmSizePtr);

            if (frmSize.type == NativeMethods.V4L2_FRMSIZE_TYPE_DISCRETE)
            {
                resolutions.Add(new Resolution((int)frmSize.size.discrete.width, (int)frmSize.size.discrete.height));
            }
            else if ((frmSize.type == NativeMethods.V4L2_FRMSIZE_TYPE_STEPWISE) || (frmSize.type == NativeMethods.V4L2_FRMSIZE_TYPE_CONTINUOUS))
            {
                AddCommonResolutions(resolutions, frmSize.size.stepwise);
                break;
            }

            index++;
        }

        return resolutions.OrderBy(static x => x.Width).ThenBy(static x => x.Height).ToList();
    }

    private static void AddCommonResolutions(List<Resolution> resolutions, NativeMethods.v4l2_frmsize_stepwise stepwise)
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

    private static Resolution SelectBestResolution(IReadOnlyList<VideoFormat> formats, uint pixelFormat = NativeMethods.V4L2_PIX_FMT_YUYV)
    {
        if (formats.Count == 0)
        {
            return new Resolution(640, 480);
        }

        var targetFormat = formats.FirstOrDefault(f => f.PixelFormat == pixelFormat) ?? formats[0];

        if (targetFormat.SupportedResolutions.Count == 0)
        {
            return new Resolution(640, 480);
        }

        var best = targetFormat.SupportedResolutions[0];
        foreach (var res in targetFormat.SupportedResolutions)
        {
            if ((res.Width * res.Height) > (best.Width * best.Height))
            {
                best = res;
            }
        }

        return best;
    }
}
