namespace Example.Video4Linux2.AvaloniaApp;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainWindowViewModel : ExtendViewModelBase
{
    public Navigator Navigator { get; set; }

    public MainWindowViewModel(Navigator navigator)
    {
        Navigator = navigator;
    }
}
