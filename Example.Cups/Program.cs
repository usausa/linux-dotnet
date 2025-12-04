// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
#pragma warning disable CA1416
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

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
    }
});
rootCommand.Add(listCommand);

// Print
var printCommand = new Command("print", "Print file");
printCommand.AddOption(new Option<string>(["--file", "-f"]) { IsRequired = true });
printCommand.AddOption(new Option<string>(["--printer", "-p"]));
printCommand.Handler = CommandHandler.Create(static (string file, string? printer) =>
{
    var jobId = CupsPrinter.PrintFile(file, printer);
    Console.WriteLine($"JobId: {jobId}");
});
rootCommand.Add(printCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
