namespace Example.GameInput.AvaloniaApp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Smart.Avalonia;
using Smart.Resolver;

public static class ApplicationExtensions
{
    //--------------------------------------------------------------------------------
    // Lifetime
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureLifetime(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IHostLifetime, NopLifetime>();

        return builder;
    }

#pragma warning disable CA1812
    private sealed class NopLifetime : IHostLifetime
    {
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
#pragma warning restore CA1812

    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureComponents(this HostApplicationBuilder builder)
    {
        builder.Services.AddAvaloniaServices();

        builder.ConfigureContainer(new SmartServiceProviderFactory(), ConfigureContainer);

        return builder;
    }

    private static void ConfigureContainer(ResolverConfig config)
    {
        config
            .UseAutoBinding()
            .UseArrayBinding()
            .UseAssignableBinding();

        // Messenger
        config.BindSingleton<IReactiveMessenger>(ReactiveMessenger.Default);

        // Window
        config.BindSingleton<MainView>();
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
