namespace Example.GameInput.AvaloniaApp.Views.Main;

public sealed partial class MenuViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public MenuViewModel()
    {
        Message = "Hello from MenuViewModel!";
    }

    protected override async ValueTask OnNavigationForwardAsync()
    {
        await Navigator.ForwardAsync(ViewId.Sub).ConfigureAwait(false);
    }
}
