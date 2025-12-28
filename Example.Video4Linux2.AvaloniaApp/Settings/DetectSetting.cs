namespace Example.Video4Linux2.AvaloniaApp.Settings;

public sealed class DetectSetting
{
    public bool Enable { get; set; }

    public string Model { get; set; } = default!;

    public bool Parallel { get; set; }

    public int IntraOpNumThreads { get; set; }

    public int InterOpNumThreads { get; set; }
}
