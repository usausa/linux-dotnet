namespace LinuxDotNet.SystemInfo;

using System;
using System.Text;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class BatteryDevice : IDisposable
{
    private const string PowerSupplyPath = "/sys/class/power_supply";

    private readonly string path;

    // The files are created only when a battery is found
    private readonly KernelFile? capacityFile;

    private readonly KernelFile? statusFile;

    private readonly KernelFile? voltageFile;

    private readonly KernelFile? currentFile;

    private readonly KernelFile? chargeFile;

    private readonly KernelFile? chargeFullFile;

    // Bytes of the current Status, to create the string only when they change
    private byte[] statusBytes = [];

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public bool Supported => !String.IsNullOrEmpty(path);

    public int Capacity { get; private set; }

    public string Status { get; private set; } = string.Empty;

    // uV
    public long Voltage { get; private set; }

    // uA
    public long Current { get; private set; }

    // uAh
    public long Charge { get; private set; }

    // uAh
    public long ChargeFull { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private BatteryDevice(string path)
    {
        this.path = path;
        if (!Supported)
        {
            return;
        }

        capacityFile = CreateValueFile(path, "capacity");
        statusFile = CreateValueFile(path, "status");
        voltageFile = CreateValueFile(path, "voltage_now");
        currentFile = CreateValueFile(path, "current_now");
        chargeFile = CreateValueFile(path, "charge_now");
        chargeFullFile = CreateValueFile(path, "charge_full");
    }

    internal static BatteryDevice Create()
    {
        var instance = new BatteryDevice(FindBattery());
        instance.Update();
        return instance;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        capacityFile?.Dispose();
        statusFile?.Dispose();
        voltageFile?.Dispose();
        currentFile?.Dispose();
        chargeFile?.Dispose();
        chargeFullFile?.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!Supported)
        {
            return false;
        }

        Capacity = ReadInt32(capacityFile);
        UpdateStatus();
        Voltage = ReadInt64(voltageFile);
        Current = ReadInt64(currentFile);
        Charge = ReadInt64(chargeFile);
        ChargeFull = ReadInt64(chargeFullFile);

        UpdateAt = DateTime.Now;

        return true;
    }

    private void UpdateStatus()
    {
        var value = (statusFile is not null) && statusFile.Read() ? TrimEnd(statusFile.Content) : default;
        if (value.SequenceEqual(statusBytes))
        {
            return;
        }

        statusBytes = value.ToArray();
        Status = Encoding.UTF8.GetString(value);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string FindBattery()
    {
        if (Directory.Exists(PowerSupplyPath))
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(PowerSupplyPath))
                {
                    var file = Path.Combine(dir, "type");
                    if (FileHelper.TryReadText(file, out var type) &&
                        type.AsSpan().Trim().StartsWith("Battery", StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Ignore
            }
        }

        return string.Empty;
    }

    private static KernelFile CreateValueFile(string path, string name) =>
        new(Path.Combine(path, name), bufferSize: 64, singleRead: true);

    // An unreadable file is 0
    private static int ReadInt32(KernelFile? file) =>
        (file is not null) && file.Read() ? ParseInt32(TrimEnd(file.Content)) : 0;

    // An unreadable file is 0
    private static long ReadInt64(KernelFile? file) =>
        (file is not null) && file.Read() ? ParseInt64(TrimEnd(file.Content)) : 0;
}
