namespace LinuxDotNet.SystemInfo;

using System.Globalization;

public enum UsbClass
{
    PerInterface = 0x00,
    Audio = 0x01,
    Communications = 0x02,
    Hid = 0x03,
    Physical = 0x05,
    Image = 0x06,
    Printer = 0x07,
    MassStorage = 0x08,
    Hub = 0x09,
    CdcData = 0x0A,
    SmartCard = 0x0B,
    ContentSecurity = 0x0D,
    Video = 0x0E,
    PersonalHealthcare = 0x0F,
    AudioVideo = 0x10,
    Billboard = 0x11,
    TypeCBridge = 0x12,
    Diagnostic = 0xDC,
    WirelessController = 0xE0,
    Miscellaneous = 0xEF,
    ApplicationSpecific = 0xFE,
    VendorSpecific = 0xFF
}

public enum UsbRemovable
{
    Unknown,
    Removable,
    Fixed
}

public sealed class UsbInterface
{
    public string Name { get; }

    public int Number { get; }

    public UsbClass InterfaceClass { get; }

    public int InterfaceSubClass { get; }

    public int InterfaceProtocol { get; }

    public string Driver { get; }

    public IReadOnlyList<string> DeviceFiles { get; }

    internal UsbInterface(string name, int number, UsbClass interfaceClass, int interfaceSubClass, int interfaceProtocol, string driver, IReadOnlyList<string> deviceFiles)
    {
        Name = name;
        Number = number;
        InterfaceClass = interfaceClass;
        InterfaceSubClass = interfaceSubClass;
        InterfaceProtocol = interfaceProtocol;
        Driver = driver;
        DeviceFiles = deviceFiles;
    }

    public override string ToString() => $"{Name} {InterfaceClass} {Driver}";
}

public sealed class UsbDevice
{
    private const string DevicesPath = "/sys/bus/usb/devices";

    private const int SearchDepth = 6;

    public string Name { get; }

    public string ParentName { get; }

    public bool IsRootHub { get; }

    public int BusNumber { get; }

    public int DeviceNumber { get; }

    public string DevicePath { get; }

    public ushort VendorId { get; }

    public ushort ProductId { get; }

    public ushort DeviceVersion { get; }

    public string Manufacturer { get; }

    public string Product { get; }

    public string SerialNumber { get; }

    public double Speed { get; }

    public string Version { get; }

    public UsbClass DeviceClass { get; }

    public int DeviceSubClass { get; }

    public int DeviceProtocol { get; }

    public int MaxPower { get; }

    public UsbRemovable Removable { get; }

    public IReadOnlyList<UsbInterface> Interfaces { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private UsbDevice(string name, IEnumerable<string> interfaceNames)
    {
        var path = Path.Combine(DevicesPath, name);

        Name = name;
        IsRootHub = name.StartsWith("usb", StringComparison.Ordinal);
        ParentName = GetParentName(name);
        BusNumber = ReadDecimal(path, "busnum");
        DeviceNumber = ReadDecimal(path, "devnum");
        DevicePath = String.Create(CultureInfo.InvariantCulture, $"/dev/bus/usb/{BusNumber:D3}/{DeviceNumber:D3}");
        VendorId = (ushort)ReadHex(path, "idVendor");
        ProductId = (ushort)ReadHex(path, "idProduct");
        DeviceVersion = (ushort)ReadHex(path, "bcdDevice");
        Manufacturer = ReadText(path, "manufacturer");
        Product = ReadText(path, "product");
        SerialNumber = ReadText(path, "serial");
        Speed = Double.TryParse(ReadText(path, "speed"), NumberStyles.Float, CultureInfo.InvariantCulture, out var speed) ? speed : 0;
        Version = ReadText(path, "version");
        DeviceClass = (UsbClass)ReadHex(path, "bDeviceClass");
        DeviceSubClass = ReadHex(path, "bDeviceSubClass");
        DeviceProtocol = ReadHex(path, "bDeviceProtocol");
        MaxPower = Int32.TryParse(ReadText(path, "bMaxPower").TrimEnd('m', 'A'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var power) ? power : 0;
        Removable = ReadText(path, "removable") switch
        {
            "removable" => UsbRemovable.Removable,
            "fixed" => UsbRemovable.Fixed,
            _ => UsbRemovable.Unknown
        };
        Interfaces = [.. interfaceNames.Order(StringComparer.Ordinal).Select(ReadInterface)];
    }

    public override string ToString() => String.Create(CultureInfo.InvariantCulture, $"{Name} {VendorId:x4}:{ProductId:x4} {Manufacturer} {Product}");

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    internal static IReadOnlyList<UsbDevice> GetDevices(bool includeRootHub)
    {
        if (!Directory.Exists(DevicesPath))
        {
            return [];
        }

        var names = Directory.EnumerateFileSystemEntries(DevicesPath)
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToList();
        var devices = new List<UsbDevice>();
        foreach (var name in names)
        {
            if (name.Contains(':', StringComparison.Ordinal) ||
                (!includeRootHub && name.StartsWith("usb", StringComparison.Ordinal)) ||
                !File.Exists(Path.Combine(DevicesPath, name, "idVendor")))
            {
                continue;
            }

            devices.Add(new UsbDevice(name, names.Where(x => x.StartsWith(name + ":", StringComparison.Ordinal))));
        }

        devices.Sort(static (x, y) => ComparePort(x.Name, y.Name));
        return devices;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static UsbInterface ReadInterface(string name)
    {
        var path = Path.Combine(DevicesPath, name);
        var files = new List<string>();
        if (ResolveDirectory(path) is { } directory)
        {
            CollectDeviceFiles(directory, SearchDepth, files);
        }

        return new UsbInterface(
            name,
            ReadHex(path, "bInterfaceNumber"),
            (UsbClass)ReadHex(path, "bInterfaceClass"),
            ReadHex(path, "bInterfaceSubClass"),
            ReadHex(path, "bInterfaceProtocol"),
            ReadLinkName(Path.Combine(path, "driver")),
            [.. files.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]);
    }

    private static void CollectDeviceFiles(DirectoryInfo directory, int depth, List<string> files)
    {
        List<DirectoryInfo> children;
        try
        {
            children = [.. directory.EnumerateDirectories()];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var child in children)
        {
            if (child.LinkTarget is not null)
            {
                continue;
            }

            if (ToDeviceFile(directory.Name, child.Name) is { } file)
            {
                files.Add(file);
            }
            else if (depth > 0)
            {
                CollectDeviceFiles(child, depth - 1, files);
            }
        }
    }

    private static string? ToDeviceFile(string parent, string name) =>
        parent switch
        {
            "video4linux" or "hidraw" or "block" => $"/dev/{name}",
            "tty" when name.StartsWith("tty", StringComparison.Ordinal) => $"/dev/{name}",
            "usbmisc" => $"/dev/usb/{name}",
            "net" => name,
            "sound" when name.StartsWith("card", StringComparison.Ordinal) => name,
            _ when parent.StartsWith("input", StringComparison.Ordinal) && IsInputNode(name) => $"/dev/input/{name}",
            _ => null
        };

    private static bool IsInputNode(string name) =>
        name.StartsWith("event", StringComparison.Ordinal) ||
        name.StartsWith("js", StringComparison.Ordinal) ||
        name.StartsWith("mouse", StringComparison.Ordinal);

    private static string GetParentName(string name)
    {
        if (name.StartsWith("usb", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var dot = name.LastIndexOf('.');
        if (dot >= 0)
        {
            return name[..dot];
        }

        var dash = name.IndexOf('-', StringComparison.Ordinal);
        return dash > 0 ? $"usb{name[..dash]}" : string.Empty;
    }

    private static int ComparePort(string x, string y)
    {
        var left = ParsePort(x);
        var right = ParsePort(y);
        for (var i = 0; i < Math.Min(left.Length, right.Length); i++)
        {
            var result = left[i].CompareTo(right[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return left.Length.CompareTo(right.Length);
    }

    private static int[] ParsePort(string name)
    {
        var port = name.StartsWith("usb", StringComparison.Ordinal) ? name[3..] : name;
        return [.. port.Split('-', '.').Select(static x => Int32.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0)];
    }

    private static DirectoryInfo? ResolveDirectory(string path)
    {
        try
        {
            var directory = new DirectoryInfo(path);
            return directory.ResolveLinkTarget(true) as DirectoryInfo ?? directory;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string ReadLinkName(string path)
    {
        try
        {
            return new DirectoryInfo(path).LinkTarget is { } target ? Path.GetFileName(target) : string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    private static string ReadText(string path, string name) => FileHelper.ReadTrimmedText(Path.Combine(path, name));

    private static int ReadDecimal(string path, string name) =>
        Int32.TryParse(ReadText(path, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static int ReadHex(string path, string name) =>
        Int32.TryParse(ReadText(path, name), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
