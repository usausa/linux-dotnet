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

var listCommand = new Command("list", "List devices");
listCommand.Handler = CommandHandler.Create(static () =>
{
    foreach (var device in EventDeviceInfo.GetDevices())
    {
        Console.WriteLine($"{device.Device,-18}  {device.VendorId}:{device.ProductId}  {device.Name}");
    }
});
rootCommand.Add(listCommand);

rootCommand.Invoke(args);
