namespace Example.GameInput.AvaloniaApp.Devices.Input;

public sealed class DebugInputDevice : IInputDevice
{
    public event EventHandler<EventArgs<InputKey>>? Handle;

    public void Trigger(InputKey key)
    {
        Handle?.Invoke(this, new EventArgs<InputKey>(key));
    }
}
