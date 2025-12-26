namespace Example.Video4Linux2.AvaloniaApp;

using Example.Video4Linux2.AvaloniaApp.Settings;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Smart.Avalonia;
using Smart.Resolver;

public static class ApplicationExtensions
{
    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureComponents(this HostApplicationBuilder builder)
    {
        builder.Services.AddAvaloniaServices();

        builder.ConfigureContainer(new SmartServiceProviderFactory(), x => ConfigureContainer(builder.Configuration, x));

        return builder;
    }

    private static void ConfigureContainer(ConfigurationManager configuration, ResolverConfig config)
    {
        config
            .UseAutoBinding()
            .UseArrayBinding()
            .UseAssignableBinding();

        // Messenger
        config.BindSingleton<IReactiveMessenger>(ReactiveMessenger.Default);

        // Setting
        config.BindConfig<CameraSetting>(configuration.GetSection("Camera"));

        // Window
        config.BindSingleton<MainWindow>();
    }

    //--------------------------------------------------------------------------------
    // Startup
    //--------------------------------------------------------------------------------

    public static async ValueTask StartApplicationAsync(this IHost host)
    {
        // Start host
        await host.StartAsync().ConfigureAwait(false);
    }

    public static async ValueTask ExitApplicationAsync(this IHost host)
    {
        // Stop host
        await host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        host.Dispose();
    }
}
