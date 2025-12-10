#pragma warning disable CA1416
namespace Example.Video4Linux2;

using LinuxDotNet.Video4Linux2;

internal static class VideoInfoHelper
{
    public static Resolution SelectBestResolution(IReadOnlyList<VideoFormat> formats, PixelFormat pixelFormat = PixelFormat.YUYV)
    {
        if (formats.Count == 0)
        {
            return new Resolution(640, 480);
        }

        var targetFormat = formats.FirstOrDefault(x => x.PixelFormat == pixelFormat) ?? formats[0];
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

    public static bool IsSuitableForCapture(VideoInfo info)
    {
        if (!info.IsVideoCapture)
        {
            return false;
        }

        if (info.SupportedFormats.Count == 0)
        {
            return false;
        }

        var hasResolutions = info.SupportedFormats.Any(static x => x.SupportedResolutions.Count > 0);
        if (!hasResolutions)
        {
            return false;
        }

        return true;
    }

    public static int CalculateDeviceScore(VideoInfo info)
    {
        // Ignore non-video-capture devices
        if (!info.IsVideoCapture)
        {
            return -500;
        }

        // Basic score calculation
        var score = info.SupportedFormats.Count * 100;

        // Device number: lower is better
        var deviceNumber = info.Device.Replace("/dev/video", string.Empty, StringComparison.Ordinal);
        if (int.TryParse(deviceNumber, out var number))
        {
            score += 100 - number;
        }

        // Has streaming capability
        if (info.IsStreaming)
        {
            score += 50;
        }

        // Higher score for more supported resolutions
        score += info.SupportedFormats.Sum(static x => x.SupportedResolutions.Count) * 10;

        // Has YUYV format support
        if (info.SupportedFormats.Any(static x => x.PixelFormat == PixelFormat.YUYV))
        {
            score += 30;
        }

        // Has MJPEG format support
        if (info.SupportedFormats.Any(static x => x.PixelFormat == PixelFormat.MJPG))
        {
            score += 20;
        }

        return score;
    }
}
