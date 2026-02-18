namespace LinuxDotNet.SystemInfo;

using System.IO;
using System.Text.RegularExpressions;

public sealed class HardwareSensor
{
    private readonly string valuePath;

    public DateTime UpdateAt { get; private set; }

    public string Type { get; }

    public string Label { get; }

    public long Value { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal HardwareSensor(string valuePath, string type, string label)
    {
        this.valuePath = valuePath;
        Type = type;
        Label = label;
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        Value = Int64.TryParse(File.ReadAllText(valuePath).AsSpan().Trim(), out var value) ? value : 0;

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed partial class HardwareMonitor
{
    public string Name { get; set; }

    public string Type { get; set; }

    public IReadOnlyList<HardwareSensor> Sensors { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal HardwareMonitor(string name, string type, IReadOnlyList<HardwareSensor> sensors)
    {
        Name = name;
        Type = type;
        Sensors = sensors;
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    internal static IReadOnlyList<HardwareMonitor> GetMonitors()
    {
        var monitors = new List<HardwareMonitor>();

        foreach (var dir in Directory.GetDirectories("/sys/class/hwmon"))
        {
            var sensors = new List<HardwareSensor>();

            var monitorName = ReadFile(Path.Combine(dir, "name"));
            var monitorType = ReadFile(Path.Combine(dir, "device/type"));

            foreach (var file in Directory.GetFiles(dir))
            {
                if (file.EndsWith("_input", StringComparison.Ordinal))
                {
                    var filename = Path.GetFileName(file);
                    var sensorType = ExtractSensorType(filename);
                    var sensorLabel = ReadFile(Path.Combine(dir, file.Replace("_input", "_label", StringComparison.Ordinal)));

                    sensors.Add(new HardwareSensor(Path.Combine(dir, file), sensorType, sensorLabel));
                }
            }

            monitors.Add(new HardwareMonitor(monitorName, monitorType, sensors));
        }

        return monitors;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string ReadFile(string path)
    {
        if (File.Exists(path))
        {
            return File.ReadAllText(path).Trim();
        }

        return string.Empty;
    }

    private static string ExtractSensorType(string filename)
    {
        var match = SensorTypePattern().Match(filename);
        return match.Success ? match.Value.TrimEnd('_') : filename;
    }

    [GeneratedRegex(@"^\d+")]
    private static partial Regex SensorTypePattern();
}
