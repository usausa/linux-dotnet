// ReSharper disable CommentTypo
// // lsusb
//
// sudo nano /etc/udev/rules.d/99-barcode.rules
// SUBSYSTEM=="input", ATTRS{idVendor}=="1130", ATTRS{idProduct}=="0001", MODE="0666"
//
// sudo apt install evtest
// sudo evtest
//
// USB-TMU-V3
using Example.InputEvent.ConsoleApp;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("InputEvent example");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
