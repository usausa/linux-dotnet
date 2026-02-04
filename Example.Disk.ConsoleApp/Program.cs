using Example.Disk.ConsoleApp;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("Disk example");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
