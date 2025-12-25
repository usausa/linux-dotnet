namespace Example.Cups.ConsoleApp;

using LinuxDotNet.Cups;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<PrinterCommand>();
        commands.AddCommand<DetailCommand>();
        commands.AddCommand<AttributeCommand>();
        commands.AddCommand<JobCommand>();
        commands.AddCommand<FileCommand>();
        commands.AddCommand<StreamCommand>();
    }
}

// Printer
[Command("printer", "List printers")]
public sealed class PrinterCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
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

        return ValueTask.CompletedTask;
    }
}

// Detail
[Command("detail", "Show details")]
public sealed class DetailCommand : ICommandHandler
{
    [Option<string>("--printer", "-p", Description = "Printer", Required = true)]
    public string Printer { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var details = CupsPrinter.GetPrinterDetail(Printer);

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

        return ValueTask.CompletedTask;
    }
}

// Attribute
[Command("attribute", "Show attributes")]
public sealed class AttributeCommand : ICommandHandler
{
    [Option<string>("--printer", "-p", Description = "Printer", Required = true)]
    public string Printer { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var attributes = CupsPrinter.GetPrinterAttributes(Printer);

        Console.WriteLine($"Printer: {Printer}");
        foreach (var key in attributes.Keys.OrderBy(static x => x))
        {
            var values = attributes[key];
            Console.WriteLine($"  {key}: {String.Join(", ", values)}");
        }

        return ValueTask.CompletedTask;
    }
}

// Job
[Command("job", "List jobs")]
public sealed class JobCommand : ICommandHandler
{
    [Option<string>("--printer", "-p", Description = "Printer")]
    public string? Printer { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        foreach (var job in CupsPrinter.GetJobs(Printer))
        {
            Console.WriteLine($"[{job.JobId}] {job.Title}");
            Console.WriteLine($"  State: {job.State}");
            Console.WriteLine($"  Printer: {job.Printer}");
            Console.WriteLine($"  DateTime: {job.SubmitTime:yyyy/MM/dd HH:mm:ss}");
        }

        return ValueTask.CompletedTask;
    }
}

// Print file
[Command("file", "Print file")]
public sealed class FileCommand : ICommandHandler
{
    [Option<string>("--file", "-f", Description = "File", Required = true)]
    public string File { get; set; } = default!;

    [Option<string>("--printer", "-p", Description = "Printer")]
    public string? Printer { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var jobId = CupsPrinter.PrintFile(File, Printer);
        Console.WriteLine($"JobId: {jobId}");

        return ValueTask.CompletedTask;
    }
}

// Print stream
[Command("stream", "Print stream")]
public sealed class StreamCommand : ICommandHandler
{
    [Option<string>("--printer", "-p", Description = "Printer")]
    public string? Printer { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var image = SampleImage.Create();
        var options = new PrintOptions
        {
            Printer = Printer,
            Copies = 1,
            MediaSize = "A4",
            ColorMode = true,
            Orientation = PrintOrientation.Portrait,
            Quality = PrintQuality.Normal
        };

        var jobId = CupsPrinter.PrintStream(image, options);
        Console.WriteLine($"JobId: {jobId}");

        return ValueTask.CompletedTask;
    }
}
