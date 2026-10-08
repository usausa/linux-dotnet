namespace LinuxDotNet.SystemInfo;

using System.IO;
using System.Text.RegularExpressions;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class HardwareSensor
{
    private readonly KernelFile file;

    private bool closed;

    public DateTime UpdateAt { get; private set; }

    public string Type { get; }

    public string Label { get; }

    public long Value { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal HardwareSensor(KernelFile file, string type, string label)
    {
        this.file = file;
        Type = type;
        Label = label;
        Update();
    }

    internal void Close()
    {
        closed = true;
        file.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(closed, this);

        if (!file.Read())
        {
            return false;
        }

        Value = ParseInt64(TrimEnd(file.Content));

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed partial class HardwareMonitor : IDisposable
{
    private const string HardwareMonitorPath = "/sys/class/hwmon";

    private bool disposed;

    public string Name { get; }

    public string Type { get; }

    public IReadOnlyList<HardwareSensor> Sensors { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private HardwareMonitor(string name, string type, IReadOnlyList<HardwareSensor> sensors)
    {
        Name = name;
        Type = type;
        Sensors = sensors;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (var sensor in Sensors)
        {
            sensor.Close();
        }
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    internal static IReadOnlyList<HardwareMonitor> GetMonitors()
    {
        if (!Directory.Exists(HardwareMonitorPath))
        {
            return [];
        }

        string[] dirs;
        try
        {
            dirs = Directory.GetDirectories(HardwareMonitorPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var monitors = new List<HardwareMonitor>();

        foreach (var dir in dirs)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(dir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var sensors = new List<HardwareSensor>();

            var monitorName = FileHelper.ReadTrimmedText(Path.Combine(dir, "name"));
            var monitorType = FileHelper.ReadTrimmedText(Path.Combine(dir, "device/type"));

            foreach (var file in files)
            {
                if (file.EndsWith("_input", StringComparison.Ordinal))
                {
                    var filename = Path.GetFileName(file);
                    var sensorType = ExtractSensorType(filename);
                    var labelPath = Path.Combine(dir, filename.Replace("_input", "_label", StringComparison.Ordinal));
                    var sensorLabel = FileHelper.ReadTrimmedText(labelPath);

                    var input = new KernelFile(file, bufferSize: 64, singleRead: true);
                    var sensor = new HardwareSensor(input, sensorType, sensorLabel);
                    if (!input.Opened)
                    {
                        input.Dispose();
                        continue;
                    }

                    sensors.Add(sensor);
                }
            }

            monitors.Add(new HardwareMonitor(monitorName, monitorType, sensors));
        }

        return monitors;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string ExtractSensorType(string filename)
    {
        var match = SensorTypePattern().Match(filename);
        return match.Success ? match.Value.TrimEnd('_') : filename;
    }

    [GeneratedRegex("^[a-zA-Z]+")]
    private static partial Regex SensorTypePattern();
}
