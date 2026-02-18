namespace LinuxDotNet.SystemInfo;

public sealed class KernelInfo
{
    public string OsType { get; }

    public string OsRelease { get; }

    public string KernelVersion { get; }

    public string? OsProductVersion { get; private set; }

    public string? OsName { get; private set; }

    public string? OsPrettyName { get; private set; }

    public string? OsId { get; private set; }

    public DateTimeOffset BootTime { get; private set; }

    public long MaxProcessCount { get; }

    public long MaxFileCount { get; }

    public long MaxFileCountPerProcess { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    // ReSharper disable StringLiteralTypo
    internal KernelInfo()
    {
        OsType = ReadProcFile("sys/kernel/ostype");
        OsRelease = ReadProcFile("sys/kernel/osrelease");
        KernelVersion = ReadProcFile("sys/kernel/version");

        ParseOsRelease();

        MaxProcessCount = ReadProcFileAsInt64("sys/kernel/pid_max");
        MaxFileCount = ReadProcFileAsInt64("sys/fs/file-max");
        MaxFileCountPerProcess = ReadProcFileAsInt64("sys/fs/nr_open");

        ParseBootTime();
    }
    // ReSharper restore StringLiteralTypo

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private void ParseOsRelease()
    {
        const string path = "/etc/os-release";
        if (!File.Exists(path))
        {
            return;
        }

        using var reader = new StreamReader(path);
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();

            var index = span.IndexOf('=');
            if (index < 0)
            {
                continue;
            }

            switch (span[..index])
            {
                case "VERSION_ID":
                    OsProductVersion = ExtractValue(line, index);
                    break;
                case "NAME":
                    OsName = ExtractValue(line, index);
                    break;
                case "PRETTY_NAME":
                    OsPrettyName = ExtractValue(line, index);
                    break;
                case "ID":
                    OsId = ExtractValue(line, index);
                    break;
            }
        }

        return;

        static string ExtractValue(ReadOnlySpan<char> span, int index)
        {
            return span[(index + 1)..].Trim('"').ToString();
        }
    }

    // ReSharper disable StringLiteralTypo
    private void ParseBootTime()
    {
        using var reader = new StreamReader("/proc/stat");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            if (span.StartsWith("btime"))
            {
                BootTime = DateTimeOffset.FromUnixTimeSeconds(ExtractInt64(span));
                return;
            }
        }

        BootTime = DateTimeOffset.MinValue;

        return;

        static long ExtractInt64(ReadOnlySpan<char> span)
        {
            var range = (Span<Range>)stackalloc Range[3];
            return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && Int64.TryParse(span[range[1]], out var result) ? result : 0;
        }
    }
    // ReSharper restore StringLiteralTypo

    private static string ReadProcFile(string file)
    {
        var path = $"/proc/{file}";
        if (File.Exists(path))
        {
            return File.ReadAllText(path).Trim();
        }

        return string.Empty;
    }

    private static long ReadProcFileAsInt64(string file)
    {
        var value = ReadProcFile(file);
        return Int64.TryParse(value, out var result) ? result : 0;
    }
}
