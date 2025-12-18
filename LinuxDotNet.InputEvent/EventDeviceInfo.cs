namespace LinuxDotNet.InputEvent;

using System.Globalization;
using System.Runtime.Versioning;

[SupportedOSPlatform("linux")]
public sealed class EventDeviceInfo
{
    private const string InputDevicePath = "/sys/class/input/";

    public string Device { get; set; }

    public string Name { get; set; }

    public int Number { get; set; }

    public string VendorId { get; set; }

    public string ProductId { get; set; }

    internal EventDeviceInfo(string device, string name, int number, string vendorId, string productId)
    {
        Device = device;
        Name = name;
        Number = number;
        VendorId = vendorId;
        ProductId = productId;
    }

    public override string ToString()
    {
        return $"{Device}: {Name} (Vendor: {VendorId}, Product: {ProductId})";
    }

    public static IReadOnlyList<EventDeviceInfo> GetDevices()
    {
        if (!Directory.Exists(InputDevicePath))
        {
            return [];
        }

        var devices = new List<EventDeviceInfo>();
        foreach (var name in Directory.GetDirectories(InputDevicePath).Select(Path.GetFileName).Where(static x => x?.StartsWith("event", StringComparison.Ordinal) ?? false).OrderBy(static x => Int32.Parse(x!.AsSpan()[5..], CultureInfo.InvariantCulture)))
        {
            var number = Int32.Parse(name.AsSpan()[5..], CultureInfo.InvariantCulture);

            devices.Add(new EventDeviceInfo(
                $"/dev/input/{name}",
                ReadFileValue(Path.Combine(InputDevicePath, name!, "device", "name")),
                number,
                ReadFileValue(Path.Combine(InputDevicePath, name!, "device", "id", "vendor")),
                ReadFileValue(Path.Combine(InputDevicePath, name!, "device", "id", "product"))));
        }

        return devices;

        static string ReadFileValue(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }
    }
}
