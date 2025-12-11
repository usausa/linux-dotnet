// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
#pragma warning disable CA1416
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using Example.Cups;

using LinuxDotNet.Cups;

var rootCommand = new RootCommand("Cups example");

// Printer
var printerCommand = new Command("printer", "List printers");
printerCommand.Handler = CommandHandler.Create(static () =>
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
rootCommand.Add(printerCommand);

// Job
var jobCommand = new Command("job", "List jobs");
jobCommand.Handler = CommandHandler.Create(static () =>
{
    // TODO Option printer, myself
    foreach (var job in CupsPrinter.GetJobs())
    {
        Console.WriteLine($"[{job.JobId}] {job.Title}");
        Console.WriteLine($"  State: {job.State}");
        Console.WriteLine($"  Printer: {job.Printer}");
        Console.WriteLine($"  DateTime: {job.SubmitTime:yyyy/MM/dd HH:mm:ss}");
    }
});
rootCommand.Add(jobCommand);

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
    File.WriteAllBytes("test.png", SampleImage.Create(1, 2).ToArray());
});
rootCommand.Add(testCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
