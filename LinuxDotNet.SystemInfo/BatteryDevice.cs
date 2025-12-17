namespace LinuxDotNet.SystemInfo;

using System;

public sealed class BatteryDevice
{
    private const string PowerSupplyPath = "/sys/class/power_supply";

    private readonly string path;

    public DateTime UpdateAt { get; private set; }

    public bool Supported => !String.IsNullOrEmpty(path);

    public int Capacity { get; private set; }

    public string Status { get; private set; } = string.Empty;

    // uV
    public long Voltage { get; private set; }

    // uA
    public long Current { get; private set; }

    // uAh
    public long Charge { get; private set; }

    // uAh
    public long ChargeFull { get; private set; }

    internal BatteryDevice()
    {
        path = FindBattery();
        Update();
    }

    public bool Update()
    {
        if (!Supported)
        {
            return false;
        }

        Capacity = ReadFileAsInt32("capacity");
        Status = ReadFile("status");
        Voltage = ReadFileAsInt64("voltage_now");
        Current = ReadFileAsInt64("current_now");
        Charge = ReadFileAsInt64("charge_now");
        ChargeFull = ReadFileAsInt64("charge_full");

        UpdateAt = DateTime.Now;

        return true;
    }

    private static string FindBattery()
    {
        if (Directory.Exists(PowerSupplyPath))
        {
            foreach (var dir in Directory.GetDirectories(PowerSupplyPath))
            {
                var file = Path.Combine(dir, "type");
                if (File.Exists(file))
                {
                    var type = File.ReadAllText(file).AsSpan().Trim();
                    if (type.StartsWith("Battery", StringComparison.OrdinalIgnoreCase))
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

    private int ReadFileAsInt32(string name)
    {
        var value = ReadFile(name);
        if (String.IsNullOrEmpty(value))
        {
            return 0;
        }

        return Int32.TryParse(value, out var result) ? result : 0;
    }

    private long ReadFileAsInt64(string name)
    {
        var value = ReadFile(name);
        if (String.IsNullOrEmpty(value))
        {
            return 0;
        }

        return Int64.TryParse(value, out var result) ? result : 0;
    }
}
