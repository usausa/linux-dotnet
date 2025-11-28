namespace Example.Video4Linux2;

using LinuxDotNet.Video4Linux2;

internal static class VideoInfoSelector
{
    // TODO

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
        var deviceNumber = info.Path.Replace("/dev/video", string.Empty, StringComparison.Ordinal);
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
        if (info.SupportedFormats.Any(static x => x.FourCC == "YUYV"))
        {
            score += 30;
        }

        // Has MJPEG format support
        if (info.SupportedFormats.Any(static x => x.FourCC == "MJPG"))
        {
            score += 20;
        }

        return score;
    }
}
