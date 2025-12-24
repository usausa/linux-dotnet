using Example.SystemInfo.ConsoleApp;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("SystemInfo example");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
