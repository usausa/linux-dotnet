namespace Example.GameInput.AvaloniaApp.Views.Main;

public sealed partial class SubViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public SubViewModel()
    {
        Message = "Hello from SubViewModel!";
    }

    protected override async ValueTask OnNavigationBackAsync()
    {
        await Navigator.ForwardAsync(ViewId.Menu).ConfigureAwait(false);
    }
}
