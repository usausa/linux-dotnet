namespace Example.GameInput.ConsoleApp;

using LinuxDotNet.GameInput;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<EventCommand>();
        commands.AddCommand<LoopCommand>();
    }
}

//--------------------------------------------------------------------------------
// Event
//--------------------------------------------------------------------------------
[Command("event", Description = "Event mode")]
public sealed class EventCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
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

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Loop
//--------------------------------------------------------------------------------
[Command("loop", Description = "Loop mode")]
public sealed class LoopCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
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

            await Task.Delay(50);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}
