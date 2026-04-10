namespace Example.Video4Linux2.AvaloniaApp;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Smart.Mvvm.Resolver;

// ReSharper disable once PartialTypeWithSinglePart
public partial class App : Application
{
    private IHost host = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        host = Host.CreateApplicationBuilder()
            .ConfigureComponents()
            .Build();
        ResolveProvider.Default.Provider = host.Services;
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Exit hook
            desktop.Exit += async (_, _) => await host.ExitApplicationAsync().ConfigureAwait(false);

            // Debug window
            desktop.MainWindow = host.Services.GetRequiredService<MainWindow>();

            // Start
            await host.StartApplicationAsync().ConfigureAwait(false);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
