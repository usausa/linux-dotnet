namespace LinuxDotNet.SystemInfo;

internal static class FileHelper
{
    public static bool TryReadText(string path, out string value)
    {
        try
        {
            value = File.ReadAllText(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            value = string.Empty;
            return false;
        }
    }

    public static bool TryReadTrimmedText(string path, out string value)
    {
        if (!TryReadText(path, out var text))
        {
            value = string.Empty;
            return false;
        }

        value = text.Trim();
        return true;
    }

    public static string ReadTrimmedText(string path)
    {
        return TryReadTrimmedText(path, out var value) ? value : string.Empty;
    }
}
