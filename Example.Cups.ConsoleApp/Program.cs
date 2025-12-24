// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
using Example.Cups.ConsoleApp;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("Cups example");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
