// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Example.Disk.ConsoleApp;

using LinuxDotNet.Disk;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<SmartCommand>();
    }
}

//--------------------------------------------------------------------------------
// Smart
//--------------------------------------------------------------------------------
[Command("smart", "Get sart")]
public sealed class SmartCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        // TODO
        SmartTest.Main(["/dev/sda"]);

        return ValueTask.CompletedTask;
    }
}
