using LinuxDotNet.GameInput;

using var controller = new GameController();

controller.ConnectionChanged += static connected =>
{
    Console.WriteLine($"Connected: {connected}");
};
controller.ButtonChanged += static (address, value) =>
{
    Console.WriteLine($"Button {address} Changed: {value}");
};
controller.AxisChanged += static (address, value) =>
{
    Console.WriteLine($"Axis {address} Changed: {value}");
};

controller.Start();

Console.ReadLine();

controller.Stop();
