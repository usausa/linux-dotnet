// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using LinuxDotNet.GameInput;

var rootCommand = new RootCommand("GameInput example");

// Event
var eventCommand = new Command("event", "Event mode");
eventCommand.Handler = CommandHandler.Create(static () =>
{
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
});
rootCommand.Add(eventCommand);

// Loop
var loopCommand = new Command("loop", "Loop mode");
loopCommand.Handler = CommandHandler.Create(static () =>
{
    Console.CursorVisible = false;
    Console.Clear();

    using var controller = new GameController();

    controller.Start();

    while (true)
    {
        Console.SetCursorPosition(0, 0);
        Console.WriteLine($"Connected: {controller.IsConnected.ToString(),-5}");
        for (var i = (byte)0; i < 8; i++)
        {
            Console.WriteLine($"Button {i}: {controller.GetButtonPressed(i).ToString(),-5}");
        }
        for (var i = (byte)0; i < 8; i++)
        {
            Console.WriteLine($"Axis {i}: {controller.GetAxisValue(i),6}");
        }

        Thread.Sleep(50);
    }
    // ReSharper disable once FunctionNeverReturns
});
rootCommand.Add(loopCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
