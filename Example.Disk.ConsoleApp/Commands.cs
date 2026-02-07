// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Example.Disk.ConsoleApp;

using LinuxDotNet.Disk;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<SmartCommand>();
    }
}

//--------------------------------------------------------------------------------
// Smart
//--------------------------------------------------------------------------------
[Command("smart", "Get sart")]
public sealed class SmartCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var disks = DiskInfo.GetInformation();
        foreach (var disk in disks)
        {
            PrintDiskInfo(disk);
            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }

    private static void PrintDiskInfo(IDiskInfo disk)
    {
        Console.WriteLine($"Disk #{disk.Index}: {disk.DeviceName}");
        Console.WriteLine($"  Model:          {disk.Model}");
        Console.WriteLine($"  Serial:         {disk.SerialNumber}");
        Console.WriteLine($"  Firmware:       {disk.FirmwareRevision}");
        Console.WriteLine($"  DiskType:       {disk.DiskType}");
        Console.WriteLine($"  SmartType:      {disk.SmartType}");
        Console.WriteLine($"  Size:           {FormatSize(disk.Size)}");
        Console.WriteLine($"  Removable:      {disk.Removable}");

        var partitions = disk.GetPartitions().ToList();
        if (partitions.Count > 0)
        {
            Console.WriteLine("  Partitions:");
            foreach (var partition in partitions)
            {
                var mountInfo = !String.IsNullOrEmpty(partition.MountPoint) ? $" -> {partition.MountPoint} ({partition.FileSystem})" : string.Empty;
                Console.WriteLine($"    {partition.Name}: {FormatSize(partition.Size)}{mountInfo}");
            }
        }

        if (disk.SmartType == SmartType.Nvme)
        {
            PrintNvmeSmart((ISmartNvme)disk.Smart);
        }
        else if (disk.SmartType == SmartType.Generic)
        {
            PrintGenericSmart((ISmartGeneric)disk.Smart);
        }
    }

    private static void PrintNvmeSmart(ISmartNvme smart)
    {
        Console.WriteLine("  SMART (NVMe):");
        Console.WriteLine($"    CriticalWarning:          {smart.CriticalWarning}");
        Console.WriteLine($"    Temperature:              {smart.Temperature}C");
        Console.WriteLine($"    AvailableSpare:           {smart.AvailableSpare}%");
        Console.WriteLine($"    AvailableSpareThreshold:  {smart.AvailableSpareThreshold}");
        Console.WriteLine($"    PercentageUsed:           {smart.PercentageUsed}%");
        Console.WriteLine($"    DataUnitRead:             {smart.DataUnitRead} ({FormatDataUnits(smart.DataUnitRead)})");
        Console.WriteLine($"    DataUnitWritten:          {smart.DataUnitWritten} ({FormatDataUnits(smart.DataUnitWritten)})");
        Console.WriteLine($"    HostReadCommands:         {smart.HostReadCommands}");
        Console.WriteLine($"    HostWriteCommands:        {smart.HostWriteCommands}");
        Console.WriteLine($"    ControllerBusyTime:       {smart.ControllerBusyTime}");
        Console.WriteLine($"    PowerCycles:              {smart.PowerCycles}");
        Console.WriteLine($"    PowerOnHours:             {smart.PowerOnHours}");
        Console.WriteLine($"    UnsafeShutdowns:          {smart.UnsafeShutdowns}");
        Console.WriteLine($"    MediaErrors:              {smart.MediaErrors}");
        Console.WriteLine($"    ErrorInfoLogEntries:      {smart.ErrorInfoLogEntries}");
        Console.WriteLine($"    WarningTemperatureTime:   {smart.WarningCompositeTemperatureTime}");
        Console.WriteLine($"    CriticalTemperatureTime:  {smart.CriticalCompositeTemperatureTime}");
        for (var i = 0; i < smart.TemperatureSensors.Length; i++)
        {
            var value = smart.TemperatureSensors[i];
            if (value > 0)
            {
                Console.WriteLine($"    TemperatureSensors-{i}:   {value}");
            }
        }
    }

    private static void PrintGenericSmart(ISmartGeneric smart)
    {
        Console.WriteLine("  SMART (Generic):");
        Console.WriteLine("    ID   FLAG   CUR  WOR  RAW");
        Console.WriteLine("    ---  ----   ---  ---  --------");

        foreach (var id in smart.GetSupportedIds())
        {
            var attr = smart.GetAttribute(id);
            if (attr.HasValue)
            {
                Console.WriteLine($"    {(byte)id,3}  0x{attr.Value.Flags:X4} {attr.Value.CurrentValue,3}  {attr.Value.WorstValue,3}  {attr.Value.RawValue}");
            }
        }
    }

    private static string FormatDataUnits(ulong units)
    {
        // 1 data unit = 512 * 1000 bytes
        var bytes = units * 512 * 1000;
        return FormatSize(bytes);
    }

    private static string FormatSize(ulong bytes)
    {
        const ulong tb = 1UL << 40;
        const ulong gb = 1UL << 30;
        const ulong mb = 1UL << 20;

        if (bytes >= tb)
        {
            return $"{(double)bytes / tb:F2} TB";
        }
        if (bytes >= gb)
        {
            return $"{(double)bytes / gb:F2} GB";
        }
        if (bytes >= mb)
        {
            return $"{(double)bytes / mb:F2} MB";
        }
        return $"{bytes} bytes";
    }
}
