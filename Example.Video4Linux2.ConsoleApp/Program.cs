using Example.Video4Linux2.ConsoleApp;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("Video4Linux2 example");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
