namespace LinuxDotNet.Disk;

using System.Globalization;

internal static class Helper
{
    public static short KelvinToCelsius(ushort value) => (short)(value > 0 ? value - 273 : Int16.MinValue);

    private static bool TryReadTrimmedText(string path, out string value)
    {
        try
        {
            value = File.ReadAllText(path).Trim();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            value = string.Empty;
            return false;
        }
    }

    public static string? ReadFile(string path)
    {
        if (!TryReadTrimmedText(path, out var content))
        {
            return null;
        }

        return String.IsNullOrWhiteSpace(content) ? null : content;
    }

    public static ulong? ReadFileAsUInt64(string path)
    {
        var str = ReadFile(path);
        return str is not null && UInt64.TryParse(str, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    public static uint? ReadFileAsUInt32(string path)
    {
        var str = ReadFile(path);
        return str is not null && UInt32.TryParse(str, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    public static bool? ReadFileAsBool(string path)
    {
        var str = ReadFile(path);
        return str switch
        {
            "1" => true,
            "0" => false,
            _ => null
        };
    }
}
