using Example.GameInput.ConsoleApp;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("GameInput example");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
