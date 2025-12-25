namespace Example.InputEvent.ConsoleApp;

using LinuxDotNet.InputEvent;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<ListCommand>();
        commands.AddCommand<RawCommand>();
        commands.AddCommand<BarcodeCommand>();
    }
}

//--------------------------------------------------------------------------------
// List
//--------------------------------------------------------------------------------
[Command("list", "List devices")]
public sealed class ListCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        foreach (var device in EventDeviceInfo.GetDevices())
        {
            Console.WriteLine($"{device.Device,-18}  {device.VendorId}:{device.ProductId}  {device.Name}");
        }

        return ValueTask.CompletedTask;
    }
}

//--------------------------------------------------------------------------------
// Raw
//--------------------------------------------------------------------------------
[Command("raw", "Raw mode")]
public sealed class RawCommand : ICommandHandler
{
    [Option<string>("--device", "-d", Description = "Device")]
    public string? Device { get; set; }

    [Option<string>("--name", "-n", Description = "Name")]
    public string? Name { get; set; }

    [Option<string>("--grab", "-g", Description = "Grab")]
    public bool Grab { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var device = Device;
        device ??= !String.IsNullOrEmpty(Name)
            ? EventDeviceInfo.GetDevices().FirstOrDefault(x => x.Name.Contains(Name, StringComparison.OrdinalIgnoreCase))?.Device
            : null;
        if (String.IsNullOrEmpty(device))
        {
            Console.WriteLine("No device.");
            return ValueTask.CompletedTask;
        }

        Console.WriteLine($"Open device. device=[{device}]");

        using var ev = new EventDevice(device);
        if (!ev.Open(Grab))
        {
            Console.WriteLine("Open failed.");
            return ValueTask.CompletedTask;
        }

        Console.WriteLine("Start read.");

        while (true)
        {
            if (ev.Read(out var result))
            {
                Console.WriteLine($"{result.Timestamp} : Type=[{result.Type}], Code=[{result.Code}], Value=[{result.Value}]");
            }
        }
    }
}

//--------------------------------------------------------------------------------
// Barcode
//--------------------------------------------------------------------------------
[Command("barcode", "Barcode mode")]
public sealed class BarcodeCommand : ICommandHandler
{
    [Option<string>("--device", "-d", Description = "Device")]
    public string? Device { get; set; }

    [Option<string>("--name", "-n", Description = "Name")]
    public string? Name { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var device = Device;
        device ??= !String.IsNullOrEmpty(Name)
            ? EventDeviceInfo.GetDevices().FirstOrDefault(x => x.Name.Contains(Name, StringComparison.OrdinalIgnoreCase))?.Device
            : null;
        if (String.IsNullOrEmpty(device))
        {
            Console.WriteLine("No device.");
            return ValueTask.CompletedTask;
        }

        Console.WriteLine($"Open device. device=[{device}]");

        using var barcode = new BarcodeReader(device);
        barcode.ConnectionChanged += static connected =>
        {
            Console.WriteLine($"Connected: {connected}");
        };
        barcode.BarcodeScanned += static code =>
        {
            Console.WriteLine($"Barcode: {code}");
        };

        barcode.Start();

        Console.ReadLine();

        barcode.Stop();

        return ValueTask.CompletedTask;
    }
}
