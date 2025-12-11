namespace LinuxDotNet.Cups;

#pragma warning disable CA1008
public enum PrintOrientation
{
    Portrait = 3,
    Landscape = 4,
    ReverseLandscape = 5,
    ReversePortrait = 6
}
#pragma warning restore CA1008

#pragma warning disable CA1008
public enum PrintQuality
{
    Draft = 3,
    Normal = 4,
    High = 5
}
#pragma warning restore CA1008

public sealed class PrintOptions
{
    internal static PrintOptions Default { get; } = new();

    public string? Printer { get; set; }

    public string JobTitle { get; set; } = "Print Job";

    public string Format { get; set; } = "image/png";

    public int Copies { get; set; } = 1;

    // "A4", "Letter", "A3", etc.
    public string? MediaSize { get; set; }

    // "plain", "photo", etc.
    public string? MediaType { get; set; }

    public bool ColorMode { get; set; } = true;

    public PrintOrientation? Orientation { get; set; }

    // "one-sided", "two-sided-long-edge", "two-sided-short-edge"
    public string? Sides { get; set; }

    public PrintQuality? Quality { get; set; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public IDictionary<string, string> CustomOptions { get; } = new Dictionary<string, string>();
}
