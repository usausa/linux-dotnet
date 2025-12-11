// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
#pragma warning disable CA1416
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using Example.Cups;

using LinuxDotNet.Cups;

var rootCommand = new RootCommand("Cups example");

// List
var listCommand = new Command("list", "List printers");
listCommand.Handler = CommandHandler.Create(static () =>
{
    foreach (var printer in CupsPrinter.GetPrinters())
    {
        Console.Write(printer.Name);
        if (printer.IsDefault)
        {
            Console.Write(" (Default)");
        }
        Console.WriteLine();

        foreach (var (name, value) in printer.Options)
        {
            Console.WriteLine($"  {name}: {value}");
        }
    }
});
rootCommand.Add(listCommand);

// Print file
var fileCommand = new Command("file", "Print file");
fileCommand.AddOption(new Option<string>(["--file", "-f"]) { IsRequired = true });
fileCommand.AddOption(new Option<string>(["--printer", "-p"]));
fileCommand.Handler = CommandHandler.Create(static (string file, string? printer) =>
{
    var jobId = CupsPrinter.PrintFile(file, printer);
    Console.WriteLine($"JobId: {jobId}");
});
rootCommand.Add(fileCommand);

// TODO image

// TODO
// Test
var testCommand = new Command("test", "Test");
testCommand.Handler = CommandHandler.Create(static () =>
{
    File.WriteAllBytes("test.png", SampleImage.Create().ToArray());
});
rootCommand.Add(testCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
