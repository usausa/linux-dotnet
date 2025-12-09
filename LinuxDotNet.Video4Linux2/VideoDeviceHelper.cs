namespace LinuxDotNet.Video4Linux2;

using System.Runtime.InteropServices;
using System.Text;

using static LinuxDotNet.Video4Linux2.NativeMethods;

internal static class VideoDeviceHelper
{
    public static IReadOnlyList<VideoFormat> GetSupportedFormats(string path)
    {
        var fd = open(path, O_RDWR);
        if (fd < 0)
        {
            throw new FileNotFoundException($"Failed to open device. path=[{path}]");
        }

        try
        {
            return GetSupportedFormats(fd);
        }
        finally
        {
            _ = close(fd);
        }
    }

    public static IReadOnlyList<VideoFormat> GetSupportedFormats(int fd)
    {
        var formats = new List<VideoFormat>();

        var index = 0u;
        while (true)
        {
            var formatDesc = new v4l2_fmtdesc
            {
                index = index,
                type = V4L2_BUF_TYPE_VIDEO_CAPTURE
            };

            var formatDescPtr = Marshal.AllocHGlobal(Marshal.SizeOf(formatDesc));
            try
            {
                Marshal.StructureToPtr(formatDesc, formatDescPtr, false);
                if (ioctl(fd, VIDIOC_ENUM_FMT, formatDescPtr) < 0)
                {
                    break;
                }

                formatDesc = Marshal.PtrToStructure<v4l2_fmtdesc>(formatDescPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(formatDescPtr);
            }

            var format = new VideoFormat(
                formatDesc.pixelformat,
                Encoding.ASCII.GetString(formatDesc.description).TrimEnd('\0'),
                GetSupportedResolutions(fd, formatDesc.pixelformat));

            formats.Add(format);
            index++;
        }

        return formats.OrderBy(static x => x.PixelFormat).ToList();
    }

    private static List<Resolution> GetSupportedResolutions(int fd, uint pixelFormat)
    {
        var resolutions = new List<Resolution>();

        var index = 0u;
        while (true)
        {
            var frmSize = new v4l2_frmsizeenum
            {
                index = index,
                pixel_format = pixelFormat
            };

            var frmSizePtr = Marshal.AllocHGlobal(Marshal.SizeOf(frmSize));
            try
            {
                Marshal.StructureToPtr(frmSize, frmSizePtr, false);
                if (ioctl(fd, VIDIOC_ENUM_FRAMESIZES, frmSizePtr) < 0)
                {
                    break;
                }

                frmSize = Marshal.PtrToStructure<v4l2_frmsizeenum>(frmSizePtr);
            }
            finally
            {
                Marshal.FreeHGlobal(frmSizePtr);
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

    //private static Resolution SelectBestResolution(IReadOnlyList<VideoFormat> formats, uint pixelFormat = V4L2_PIX_FMT_YUYV)
    //{
    //    if (formats.Count == 0)
    //    {
    //        return new Resolution(640, 480);
    //    }

    //    var targetFormat = formats.FirstOrDefault(f => f.PixelFormat == pixelFormat) ?? formats[0];

    //    if (targetFormat.SupportedResolutions.Count == 0)
    //    {
    //        return new Resolution(640, 480);
    //    }

    //    var best = targetFormat.SupportedResolutions[0];
    //    foreach (var res in targetFormat.SupportedResolutions)
    //    {
    //        if ((res.Width * res.Height) > (best.Width * best.Height))
    //        {
    //            best = res;
    //        }
    //    }

    //    return best;
    //}
}
