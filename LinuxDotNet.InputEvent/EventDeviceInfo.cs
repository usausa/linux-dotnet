namespace LinuxDotNet.InputEvent;

using System.Globalization;

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
        foreach (var name in Directory.EnumerateDirectories(InputDevicePath).Select(Path.GetFileName))
        {
            if ((name is null) || !name.StartsWith("event", StringComparison.Ordinal))
            {
                continue;
            }

            if (!Int32.TryParse(name.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                continue;
            }

            devices.Add(new EventDeviceInfo(
                $"/dev/input/{name}",
                ReadFileValue(Path.Combine(InputDevicePath, name, "device", "name")),
                number,
                ReadFileValue(Path.Combine(InputDevicePath, name, "device", "id", "vendor")),
                ReadFileValue(Path.Combine(InputDevicePath, name, "device", "id", "product"))));
        }

        return devices.OrderBy(static x => x.Number).ToList();

        static string ReadFileValue(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }
    }
}
