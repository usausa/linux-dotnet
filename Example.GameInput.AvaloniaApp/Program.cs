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
        if (args.Length > 0)
        {
            if (args[0].StartsWith("/dev/fb", StringComparison.Ordinal))
            {
                return builder.StartLinuxFbDev(args, args[0]);
            }
            if (args[0].StartsWith("/dev/dri/card", StringComparison.Ordinal))
            {
                return builder.StartLinuxDrm(args, args[0], 1);
            }
        }
        return -1;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .WithInterFont()
            .LogToTrace();
}
