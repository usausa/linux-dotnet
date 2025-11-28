namespace Example.Video4Linux2;

using LinuxDotNet.Video4Linux2;

internal static class CameraDeviceSelector
{
    // TODO

    public static bool IsSuitableForCapture(VideoDevice device)
    {
        if (!device.IsVideoCapture)
        {
            return false;
        }

        if (device.SupportedFormats.Count == 0)
        {
            return false;
        }

        var hasResolutions = device.SupportedFormats.Any(static x => x.SupportedResolutions.Count > 0);
        if (!hasResolutions)
        {
            return false;
        }

        return true;
    }

    public static int CalculateDeviceScore(VideoDevice device)
    {
        // Ignore non-video-capture devices
        if (!device.IsVideoCapture)
        {
            return -500;
        }

        // Basic score calculation
        var score = device.SupportedFormats.Count * 100;

        // Device number: lower is better
        var deviceNumber = device.Path.Replace("/dev/video", string.Empty, StringComparison.Ordinal);
        if (int.TryParse(deviceNumber, out var number))
        {
            score += 100 - number;
        }

        // Has streaming capability
        if (device.IsStreaming)
        {
            score += 50;
        }

        // Higher score for more supported resolutions
        score += device.SupportedFormats.Sum(static x => x.SupportedResolutions.Count) * 10;

        // Has YUYV format support
        if (device.SupportedFormats.Any(static x => x.FourCC == "YUYV"))
        {
            score += 30;
        }

        // Has MJPEG format support
        if (device.SupportedFormats.Any(static x => x.FourCC == "MJPG"))
        {
            score += 20;
        }

        return score;
    }
}
