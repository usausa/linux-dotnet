namespace LinuxDotNet.Cups;

public sealed class PrinterInfo
{
    public string Name { get; }

    public bool IsDefault { get; }

    public PrinterInfo(string name, bool isDefault)
    {
        Name = name;
        IsDefault = isDefault;
    }
}
