namespace LinuxDotNet.SystemInfo;

using System;

public sealed class MainsAdapterDevice
{
    private const string PowerSupplyPath = "/sys/class/power_supply/";

    private readonly string path;

    public DateTime UpdateAt { get; private set; }

    public bool Supported => !String.IsNullOrEmpty(path);

    public bool Online { get; private set; }

    internal MainsAdapterDevice()
    {
        path = FindAdapter();
        Update();
    }

    public bool Update()
    {
        if (!Supported)
        {
            return false;
        }

        Online = ReadFile("online") == "1";

        UpdateAt = DateTime.Now;

        return true;
    }

    private static string FindAdapter()
    {
        if (Directory.Exists(PowerSupplyPath))
        {
            foreach (var dir in Directory.GetDirectories(PowerSupplyPath))
            {
                var file = Path.Combine(dir, "type");
                if (File.Exists(file))
                {
                    var type = File.ReadAllText(file).AsSpan().Trim();
                    if (type.StartsWith("Mains", StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }
            }
        }

        return string.Empty;
    }

    private string ReadFile(string name)
    {
        var file = Path.Combine(path, name);
        if (File.Exists(file))
        {
            return File.ReadAllText(file).Trim();
        }

        return string.Empty;
    }
}
