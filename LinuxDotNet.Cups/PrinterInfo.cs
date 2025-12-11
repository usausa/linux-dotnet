namespace LinuxDotNet.Cups;

public sealed class PrinterInfo
{
    public string Name { get; }

    public string? Instance { get; }

    public bool IsDefault { get; }

    public Dictionary<string, string> Options { get; } = new();

    public PrinterInfo(string name, string? instance, bool isDefault)
    {
        Name = name;
        Instance = instance;
        IsDefault = isDefault;
    }
}
