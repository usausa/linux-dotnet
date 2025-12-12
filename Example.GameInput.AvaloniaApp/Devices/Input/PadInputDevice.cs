namespace Example.GameInput.AvaloniaApp.Devices.Input;

using Gamepad;

public sealed class PadInputDevice : IInputDevice, IDisposable
{
    public event EventHandler<EventArgs<InputKey>>? Handle;

    private readonly GamepadController controller = new();

    public PadInputDevice()
    {
        controller.ButtonChanged += (_, args) =>
        {
            if (!args.Pressed)
            {
                var key = args.Button switch
                {
                    0 => InputKey.Button1,
                    1 => InputKey.Button2,
                    2 => InputKey.Button3,
                    3 => InputKey.Button4,
                    _ => InputKey.Unknown
                };

                Handle?.Invoke(this, new EventArgs<InputKey>(key));
            }
        };
    }

    public void Dispose()
    {
        controller.Dispose();
    }
}
