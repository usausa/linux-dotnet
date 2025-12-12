namespace Example.Video4Linux2.AvaloniaApp.Views;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public abstract class AppViewModelBase : ExtendViewModelBase, INavigatorAware, INavigationEventSupport
{
    public INavigator Navigator { get; set; } = default!;

    public void OnNavigatingFrom(INavigationContext context)
    {
    }

    public void OnNavigatingTo(INavigationContext context)
    {
    }

    public void OnNavigatedTo(INavigationContext context)
    {
    }
}
