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

// Detail
var detailCommand = new Command("detail", "Show details");
detailCommand.AddOption(new Option<string>(["--printer", "-p"], "Printer") { IsRequired = true });
detailCommand.Handler = CommandHandler.Create(static (string printer) =>
{
    var details = CupsPrinter.GetPrinterDetail(printer);

    Console.WriteLine($"Printer: {details.Name}");
    Console.WriteLine($"  Description: {details.Description}");
    Console.WriteLine($"  Location: {details.Location}");
    Console.WriteLine($"  MakeModel: {details.MakeModel}");
    Console.WriteLine($"  State: {details.State}");
    Console.WriteLine($"  Message: {details.StateMessage}");
    Console.WriteLine($"  Accepting: {details.IsAcceptingJobs}");
    Console.WriteLine($"  MediaSizes: {String.Join(", ", details.SupportedMediaSizes)}");
    Console.WriteLine($"  MediaTypes: {String.Join(", ", details.SupportedMediaTypes)}");
    Console.WriteLine($"  Resolutions: {String.Join(", ", details.SupportedResolutions.Select(static x => $"{x.XResolution}x{x.YResolution}{x.Units}"))}");
    Console.WriteLine($"  Color: {details.SupportsColor}");
    Console.WriteLine($"  Duplex: {details.SupportsDuplex}");
});
rootCommand.Add(detailCommand);

// Attribute
var attributeCommand = new Command("attribute", "Show attributes");
attributeCommand.AddOption(new Option<string>(["--printer", "-p"], "Printer") { IsRequired = true });
attributeCommand.Handler = CommandHandler.Create(static (string printer) =>
{
    var attributes = CupsPrinter.GetPrinterAttributes(printer);

    Console.WriteLine($"Printer: {printer}");
    foreach (var key in attributes.Keys.OrderBy(static x => x))
    {
        var values = attributes[key];
        Console.WriteLine($"  {key}: {String.Join(", ", values)}");
    }
});
rootCommand.Add(attributeCommand);

// Job
var jobCommand = new Command("job", "List jobs");
jobCommand.AddOption(new Option<string>(["--printer", "-p"]));
jobCommand.Handler = CommandHandler.Create(static (string? printer) =>
{
    foreach (var job in CupsPrinter.GetJobs(printer))
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

// Print direct
var directCommand = new Command("direct", "Print direct");
directCommand.AddOption(new Option<string>(["--printer", "-p"]));
directCommand.Handler = CommandHandler.Create(static () =>
{
    // TODO printer option
    // TODO image
    File.WriteAllBytes("test.png", SampleImage.Create(1, 2).ToArray());
});
rootCommand.Add(directCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
