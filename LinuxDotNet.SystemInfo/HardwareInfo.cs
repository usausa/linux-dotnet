namespace LinuxDotNet.SystemInfo;

public sealed class HardwareInfo
{
    // DMI

    public string Model { get; }

    public string Vendor { get; }

    public string ProductName { get; }

    public string? ProductVersion { get; }

    public string? SerialNumber { get; }

    // BIOS

    public string? BiosVendor { get; }

    public string? BiosVersion { get; }

    public string? BiosDate { get; }

    public string? BiosRelease { get; }

    // Architecture

    public string Architecture { get; }

    // CPU

    public string? CpuBrandString { get; private set; }

    public string? CpuVendor { get; private set; }

    public int CpuFamily { get; private set; }

    public int CpuModel { get; private set; }

    public int CpuStepping { get; private set; }

    public int LogicalCpu { get; private set; }

    public int PhysicalCpu { get; private set; }

    public int CoresPerSocket { get; private set; }

    public long CpuFrequency { get; private set; }

    public long CpuFrequencyMax { get; private set; }

    public long CacheLineSize { get; private set; }

    public long L1DCacheSize { get; private set; }

    public long L1ICacheSize { get; private set; }

    public long L2CacheSize { get; private set; }

    public long L3CacheSize { get; private set; }

    // Memory

    public long MemSize { get; }

    // PageSize

    public long PageSize { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal HardwareInfo()
    {
        Model = ReadDmiFile("product_name");
        Vendor = ReadDmiFile("sys_vendor");
        ProductName = ReadDmiFile("product_name");
        ProductVersion = ReadDmiFileOrNull("product_version");
        SerialNumber = ReadDmiFileOrNull("product_serial");

        BiosVendor = ReadDmiFileOrNull("bios_vendor");
        BiosVersion = ReadDmiFileOrNull("bios_version");
        BiosDate = ReadDmiFileOrNull("bios_date");
        BiosRelease = ReadDmiFileOrNull("bios_release");

        Architecture = ReadFileOrNull("/proc/sys/kernel/arch") ?? Environment.GetEnvironmentVariable("HOSTTYPE") ?? "unknown";

        ParseCpuInfo();

        MemSize = ReadMemInfo("MemTotal") * 1024;

        PageSize = Environment.SystemPageSize;
    }

    private void ParseCpuInfo()
    {
        // TODO
        var physicalIds = new HashSet<int>();
        var processors = 0;
        var coresPerSocket = 0;

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
                case "model name":
                    CpuBrandString ??= value.ToString();
                    break;
                case "vendor_id":
                    CpuVendor ??= value.ToString();
                    break;
                case "cpu family":
                    if (CpuFamily == 0 && Int32.TryParse(value, out var family))
                    {
                        CpuFamily = family;
                    }

                    break;
                case "model":
                    if (CpuModel == 0 && Int32.TryParse(value, out var model))
                    {
                        CpuModel = model;
                    }

                    break;
                case "stepping":
                    if (CpuStepping == 0 && Int32.TryParse(value, out var stepping))
                    {
                        CpuStepping = stepping;
                    }

                    break;
                case "processor":
                    processors++;
                    break;
                case "physical id":
                    if (Int32.TryParse(value, out var physId))
                    {
                        physicalIds.Add(physId);
                    }

                    break;
                case "cpu cores":
                    if (coresPerSocket == 0 && Int32.TryParse(value, out var cores))
                    {
                        coresPerSocket = cores;
                    }

                    break;
                case "cpu MHz":
                    if (CpuFrequency == 0 && Double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mhz))
                    {
                        CpuFrequency = (long)(mhz * 1_000_000);
                    }

                    break;
                case "clflush size":
                    if (CacheLineSize == 0 && Int64.TryParse(value, out var clflush))
                    {
                        CacheLineSize = clflush;
                    }

                    break;
            }
        }

        LogicalCpu = processors;
        PhysicalCpu = physicalIds.Count > 0 ? physicalIds.Count : (processors > 0 ? 1 : 0);
        CoresPerSocket = coresPerSocket;

        CpuFrequencyMax = ReadCpuFrequencyMax();

        ParseCacheInfo();
    }

    private void ParseCacheInfo()
    {
        var cacheBasePath = "/sys/devices/system/cpu/cpu0/cache";
        if (!Directory.Exists(cacheBasePath))
        {
            return;
        }

        foreach (var indexDir in Directory.GetDirectories(cacheBasePath, "index*"))
        {
            var levelStr = ReadFileOrNull(Path.Combine(indexDir, "level"));
            var typeStr = ReadFileOrNull(Path.Combine(indexDir, "type"));
            var sizeStr = ReadFileOrNull(Path.Combine(indexDir, "size"));

            if (!Int32.TryParse(levelStr, out var level))
            {
                continue;
            }

            var sizeKb = ParseCacheSize(sizeStr);

            switch (level)
            {
                case 1 when typeStr?.Contains("Data", StringComparison.OrdinalIgnoreCase) == true:
                    L1DCacheSize = sizeKb * 1024;
                    break;
                case 1 when typeStr?.Contains("Instruction", StringComparison.OrdinalIgnoreCase) == true:
                    L1ICacheSize = sizeKb * 1024;
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

    private static long ParseCacheSize(string? sizeStr)
    {
        if (string.IsNullOrEmpty(sizeStr))
        {
            return 0;
        }

        sizeStr = sizeStr.Trim().ToUpperInvariant();
        if (sizeStr.EndsWith('K'))
        {
            return Int64.TryParse(sizeStr[..^1], out var kb) ? kb : 0;
        }

        if (sizeStr.EndsWith('M'))
        {
            return Int64.TryParse(sizeStr[..^1], out var mb) ? mb * 1024 : 0;
        }

        return Int64.TryParse(sizeStr, out var bytes) ? bytes / 1024 : 0;
    }

    // ReSharper disable StringLiteralTypo
    private static long ReadCpuFrequencyMax()
    {
        var path = "/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq";
        if (File.Exists(path))
        {
            var value = File.ReadAllText(path).Trim();
            if (Int64.TryParse(value, out var khz))
            {
                return khz * 1000;
            }
        }

        return 0;
    }
    // ReSharper restore StringLiteralTypo

    private static long ReadMemInfo(string key)
    {
        using var reader = new StreamReader("/proc/meminfo");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            if (span.StartsWith(key))
            {
                var colonIndex = span.IndexOf(':');
                if (colonIndex >= 0)
                {
                    var valueSpan = span[(colonIndex + 1)..].Trim();
                    var range = (Span<Range>)stackalloc Range[2];
                    if (valueSpan.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 0 && Int64.TryParse(valueSpan[range[0]], out var value))
                    {
                        return value;
                    }
                }
            }
        }

        return 0;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string ReadDmiFile(string name)
    {
        return ReadDmiFileOrNull(name) ?? string.Empty;
    }

    private static string? ReadDmiFileOrNull(string name)
    {
        var path = $"/sys/class/dmi/id/{name}";
#pragma warning disable CA1031
        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim();
            }
        }
        catch
        {
            // Ignore
        }
#pragma warning restore CA1031

        return null;
    }

    private static string? ReadFileOrNull(string path)
    {
        if (File.Exists(path))
        {
            return File.ReadAllText(path).Trim();
        }

        return null;
    }
}
