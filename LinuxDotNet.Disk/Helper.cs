namespace LinuxDotNet.Disk;

internal static class Helper
{
    public static short KelvinToCelsius(ushort value) => (short)(value > 0 ? value - 273 : Int16.MinValue);

    public static string? ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var content = File.ReadAllText(path).Trim();
        return String.IsNullOrWhiteSpace(content) ? null : content;
    }

    public static ulong? ReadFileAsUInt64(string path)
    {
        var str = ReadFile(path);
        return str is not null && UInt64.TryParse(str, out var value) ? value : null;
    }

    public static uint? ReadFileAsUInt32(string path)
    {
        var str = ReadFile(path);
        return str is not null && UInt32.TryParse(str, out var value) ? value : null;
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
