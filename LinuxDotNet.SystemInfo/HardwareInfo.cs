namespace LinuxDotNet.SystemInfo;

using System.Globalization;

public sealed class HardwareInfo
{
    // DMI

    public string Vendor { get; }

    public string ProductName { get; }

    public string ProductVersion { get; }

    public string SerialNumber { get; }

    public string BiosVendor { get; }

    public string BiosVersion { get; }

    public string BiosDate { get; }

    public string BiosRelease { get; }

    public string BoardName { get; }

    public string BoardVendor { get; }

    public string BoardVersion { get; }

    // CPU

    public string CpuBrandString { get; private set; } = string.Empty;

    public string CpuVendor { get; private set; } = string.Empty;

    public int CpuFamily { get; private set; }

    public int CpuModel { get; private set; }

    public int CpuStepping { get; private set; }

    public int LogicalCpu { get; private set; }

    public int PhysicalCpu { get; private set; }

    public int CoresPerSocket { get; private set; }

    public ulong CpuFrequencyMax { get; private set; }

    public ulong L1DCacheSize { get; private set; }

    public ulong L1ICacheSize { get; private set; }

    public ulong L2CacheSize { get; private set; }

    public ulong L3CacheSize { get; private set; }

    // Memory

    public ulong MemoryTotal { get; }

    // PageSize

    public ulong PageSize { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal HardwareInfo()
    {
        Vendor = ReadDmiFile("sys_vendor");
        ProductName = ReadDmiFile("product_name");
        ProductVersion = ReadDmiFile("product_version");
        SerialNumber = ReadDmiFile("product_serial");

        BiosVendor = ReadDmiFile("bios_vendor");
        BiosVersion = ReadDmiFile("bios_version");
        BiosDate = ReadDmiFile("bios_date");
        BiosRelease = ReadDmiFile("bios_release");

        BoardName = ReadDmiFile("board_name");
        BoardVendor = ReadDmiFile("board_vendor");
        BoardVersion = ReadDmiFile("board_version");

        ParseCpuInfo();

        MemoryTotal = ReadMemoryTotal();

        PageSize = (ulong)Environment.SystemPageSize;
    }

    private void ParseCpuInfo()
    {
        var physicalIds = new HashSet<int>();
        var processors = 0;

        using var reader = new StreamReader("/proc/cpuinfo");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            var colonIndex = span.IndexOf(':');
            if (colonIndex < 0)
            {
                continue;
            }

            var key = span[..colonIndex].Trim().ToString();
            var value = span[(colonIndex + 1)..].Trim();

            switch (key)
            {
                case "processor":
                    processors++;
                    continue;
                case "physical id":
                    if (Int32.TryParse(value, CultureInfo.InvariantCulture, out var physId))
                    {
                        physicalIds.Add(physId);
                    }
                    continue;
            }

            if (processors <= 1)
            {
                switch (key)
                {
                    case "model name":
                        CpuBrandString = value.ToString();
                        break;
                    case "vendor_id":
                        CpuVendor = value.ToString();
                        break;
                    case "cpu family":
                        CpuFamily = Int32.TryParse(value, CultureInfo.InvariantCulture, out var family) ? family : 0;
                        break;
                    case "model":
                        CpuModel = Int32.TryParse(value, CultureInfo.InvariantCulture, out var model) ? model : 0;
                        break;
                    case "stepping":
                        CpuStepping = Int32.TryParse(value, CultureInfo.InvariantCulture, out var stepping) ? stepping : 0;
                        break;
                    case "cpu cores":
                        CoresPerSocket = Int32.TryParse(value, CultureInfo.InvariantCulture, out var cores) ? cores : 0;
                        break;
                }
            }
        }

        LogicalCpu = processors;
        PhysicalCpu = physicalIds.Count > 0 ? physicalIds.Count : (processors > 0 ? 1 : 0);

        CpuFrequencyMax = ReadCpuFrequencyMax();

        ParseCacheInfo();
    }

    // ReSharper disable StringLiteralTypo
    private static ulong ReadCpuFrequencyMax()
    {
        if (FileHelper.TryReadText("/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq", out var value) &&
            UInt64.TryParse(value.AsSpan().Trim(), CultureInfo.InvariantCulture, out var khz))
        {
            return khz * 1000;
        }

        return 0;
    }
    // ReSharper restore StringLiteralTypo

    private void ParseCacheInfo()
    {
        var cacheBasePath = "/sys/devices/system/cpu/cpu0/cache";
        if (!Directory.Exists(cacheBasePath))
        {
            return;
        }

        foreach (var indexDir in Directory.GetDirectories(cacheBasePath, "index*"))
        {
            if (!Int32.TryParse(FileHelper.ReadTrimmedText(Path.Combine(indexDir, "level")), CultureInfo.InvariantCulture, out var level))
            {
                continue;
            }

            var sizeKb = ParseCacheSize(FileHelper.ReadTrimmedText(Path.Combine(indexDir, "size")));

            switch (level)
            {
                case 1:
                    var type = FileHelper.ReadTrimmedText(Path.Combine(indexDir, "type"));
                    if (type.Contains("Data", StringComparison.OrdinalIgnoreCase))
                    {
                        L1DCacheSize = sizeKb * 1024;
                    }
                    else if (type.Contains("Instruction", StringComparison.OrdinalIgnoreCase))
                    {
                        L1ICacheSize = sizeKb * 1024;
                    }
                    break;
                case 2:
                    L2CacheSize = sizeKb * 1024;
                    break;
                case 3:
                    L3CacheSize = sizeKb * 1024;
                    break;
            }
        }
    }

    private static ulong ParseCacheSize(string size)
    {
        if (String.IsNullOrEmpty(size))
        {
            return 0;
        }

        size = size.Trim().ToUpperInvariant();
        if (size.EndsWith('K'))
        {
            return UInt64.TryParse(size[..^1], CultureInfo.InvariantCulture, out var kb) ? kb : 0;
        }

        if (size.EndsWith('M'))
        {
            return UInt64.TryParse(size[..^1], CultureInfo.InvariantCulture, out var mb) ? mb * 1024 : 0;
        }

        return UInt64.TryParse(size, CultureInfo.InvariantCulture, out var bytes) ? bytes / 1024 : 0;
    }

    private static ulong ReadMemoryTotal()
    {
        using var reader = new StreamReader("/proc/meminfo");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            if (span.StartsWith("MemTotal:"))
            {
                return ExtractUInt64(span) * 1024;
            }
        }

        return 0;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string ReadDmiFile(string name)
    {
        return FileHelper.ReadTrimmedText($"/sys/class/dmi/id/{name}");
    }

    private static ulong ExtractUInt64(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && UInt64.TryParse(span[range[1]], CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
