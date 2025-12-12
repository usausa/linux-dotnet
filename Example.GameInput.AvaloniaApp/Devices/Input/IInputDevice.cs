namespace Example.GameInput.AvaloniaApp.Devices.Input;

using System;

public interface IInputDevice
{
    event EventHandler<EventArgs<InputKey>> Handle;
}
