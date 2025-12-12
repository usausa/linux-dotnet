namespace Example.GameInput.AvaloniaApp;

using System;

using Avalonia;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var builder = BuildAvaloniaApp();
#if DEBUG
        return builder.StartWithClassicDesktopLifetime(args);
#else
        return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
#endif
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .WithInterFont()
            .LogToTrace();
}
