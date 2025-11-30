using Gamepad;

using var pad = new GamepadController();

pad.ButtonChanged += static (_, e) =>
{
    Console.WriteLine($"Button {e.Button} Changed: {e.Pressed}");
};
pad.AxisChanged += static (_, e) =>
{
    Console.WriteLine($"Axis {e.Axis} Changed: {e.Value}");
};

Console.ReadLine();
