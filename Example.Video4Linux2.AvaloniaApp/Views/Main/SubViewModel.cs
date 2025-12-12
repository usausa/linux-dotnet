namespace Example.Video4Linux2.AvaloniaApp.Views.Main;

public sealed partial class SubViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public ICommand NavigateCommand { get; }

    public SubViewModel()
    {
        Message = "Hello from SubViewModel!";
        NavigateCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Menu);
        });
    }
}
