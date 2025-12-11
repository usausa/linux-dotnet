namespace LinuxDotNet.Cups;

public sealed class PrintOptions
{
    public string? Printer { get; set; }

    public string JobTitle { get; set; } = "Print Job";

    public int Copies { get; set; } = 1;

    // "A4", "Letter", "A3", etc.
    public string? MediaSize { get; set; }

    // "plain", "photo", etc.
    public string? MediaType { get; set; }

    public bool ColorMode { get; set; } = true;

    // TODO ?
    // "portrait", "landscape"
    public string? Orientation { get; set; }

    // TODO
    // "one-sided", "two-sided-long-edge", "two-sided-short-edge"
    public string? Sides { get; set; }

    // TODO
    // 3=draft, 4=normal, 5=high
    public int? Quality { get; set; }

    public IDictionary<string, string> CustomOptions { get; } = new Dictionary<string, string>();
}
