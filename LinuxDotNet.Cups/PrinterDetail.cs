namespace LinuxDotNet.Cups;

public enum PrinterState
{
    Unknown = 0,
    Idle = 3,
    Processing = 4,
    Stopped = 5
}

public sealed class PrinterResolution
{
    public int XResolution { get; set; }

    public int YResolution { get; set; }

    public string Units { get; set; } = "dpi";
}

public sealed class PrinterDetail
{
    public string Name { get; }

    public string Description { get; }

    public string Location { get; }

    public string MakeModel { get; }

    public PrinterState State { get; }

    public string StateMessage { get; }

    public bool IsAcceptingJobs { get; }

    public IReadOnlyList<string> SupportedMediaSizes { get; }

    public IReadOnlyList<string> SupportedMediaTypes { get; }

    public IReadOnlyList<PrinterResolution> SupportedResolutions { get; }

    public bool SupportsColor { get; }

    public bool SupportsDuplex { get; }

    public PrinterDetail(
        string name,
        string description,
        string location,
        string makeModel,
        PrinterState state,
        string stateMessage,
        bool isAcceptingJobs,
        IReadOnlyList<string> supportedMediaSizes,
        IReadOnlyList<string> supportedMediaTypes,
        IReadOnlyList<PrinterResolution> supportedResolutions,
        bool supportsColor,
        bool supportsDuplex)
    {
        Name = name;
        Description = description;
        Location = location;
        MakeModel = makeModel;
        State = state;
        StateMessage = stateMessage;
        IsAcceptingJobs = isAcceptingJobs;
        SupportedMediaSizes = supportedMediaSizes;
        SupportedMediaTypes = supportedMediaTypes;
        SupportedResolutions = supportedResolutions;
        SupportsColor = supportsColor;
        SupportsDuplex = supportsDuplex;
    }
}
