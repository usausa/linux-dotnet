namespace Example.GameInput.AvaloniaApp;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
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
            .ConfigureLogging()
            .ConfigureLifetime()
            .ConfigureComponents()
            .Build();
        ResolveProvider.Default.Provider = host.Services;
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Main view
            singleViewPlatform.MainView = host.Services.GetRequiredService<MainView>();

            // Start
            await host.StartApplicationAsync().ConfigureAwait(false);
        }
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Exit hook
            desktop.Exit += async (_, _) => await host.ExitApplicationAsync().ConfigureAwait(false);

            // Debug window
            desktop.MainWindow = host.Services.GetRequiredService<DebugWindow>();

            // Start
            await host.StartApplicationAsync().ConfigureAwait(false);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
