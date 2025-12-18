#pragma warning disable CA1416
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable UseObjectOrCollectionInitializer

// lsusb
//
// sudo nano /etc/udev/rules.d/99-barcode.rules
// SUBSYSTEM=="input", ATTRS{idVendor}=="1130", ATTRS{idProduct}=="0001", MODE="0666"
//
// sudo apt install evtest
// sudo evtest
//
// USB-TMU-V3

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using LinuxDotNet.InputEvent;

var rootCommand = new RootCommand("Input example");

//--------------------------------------------------------------------------------
var listCommand = new Command("list", "List devices");
//--------------------------------------------------------------------------------
listCommand.Handler = CommandHandler.Create(static () =>
{
    foreach (var device in EventDeviceInfo.GetDevices())
    {
        Console.WriteLine($"{device.Device,-18}  {device.VendorId}:{device.ProductId}  {device.Name}");
    }
});
rootCommand.Add(listCommand);

//--------------------------------------------------------------------------------
var rawCommand = new Command("raw", "Raw mode");
//--------------------------------------------------------------------------------
rawCommand.AddOption(new Option<string>(["--device", "-d"], "Device"));
rawCommand.AddOption(new Option<string>(["--name", "-n"], () => string.Empty, "Name"));
rawCommand.AddOption(new Option<bool>(["--grab", "-g"], () => false, "Grab"));
rawCommand.Handler = CommandHandler.Create(static (string? device, string name, bool grab) =>
{
    device ??= !String.IsNullOrEmpty(name)
        ? EventDeviceInfo.GetDevices().FirstOrDefault(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase))?.Device
        : null;
    if (String.IsNullOrEmpty(device))
    {
        Console.WriteLine("No device.");
        return;
    }

    Console.WriteLine($"Open device. device=[{device}]");

    using var ev = new EventDevice(device);
    if (!ev.Open(grab))
    {
        Console.WriteLine("Open failed.");
        return;
    }

    Console.WriteLine("Start read.");

    while (true)
    {
        if (ev.Read(out var result))
        {
            Console.WriteLine($"{result.Timestamp} : Type=[{result.Type}], Code=[{result.Code}], Value=[{result.Value}]");
        }
    }
});
rootCommand.Add(rawCommand);

rootCommand.Invoke(args);
