namespace LinuxDotNet.Disk;

using static LinuxDotNet.Disk.Helper;
using static LinuxDotNet.Disk.NativeMethods;

internal sealed class SmartNvme : ISmartNvme, IDisposable
{
    private int fd;

    public bool LastUpdate { get; private set; }

    public byte CriticalWarning { get; private set; }

    public short Temperature { get; private set; }

    public byte AvailableSpare { get; private set; }

    public byte AvailableSpareThreshold { get; private set; }

    public byte PercentageUsed { get; private set; }

    public ulong DataUnitRead { get; private set; }

    public ulong DataUnitWritten { get; private set; }

    public ulong HostReadCommands { get; private set; }

    public ulong HostWriteCommands { get; private set; }

    public ulong ControllerBusyTime { get; private set; }

    public ulong PowerCycles { get; private set; }

    public ulong PowerOnHours { get; private set; }

    public ulong UnsafeShutdowns { get; private set; }

    public ulong MediaErrors { get; private set; }

    public ulong ErrorInfoLogEntries { get; private set; }

    public uint WarningCompositeTemperatureTime { get; private set; }

    public uint CriticalCompositeTemperatureTime { get; private set; }

    public short[] TemperatureSensors { get; } = new short[8];

    public SmartNvme(string devicePath)
    {
        fd = open(devicePath, O_RDONLY);
    }

    public void Dispose()
    {
        if (fd >= 0)
        {
            _ = close(fd);
            fd = -1;
        }
    }

    public unsafe bool Update()
    {
        if (fd < 0)
        {
            LastUpdate = false;
            return false;
        }

        nvme_smart_log smartLog = default;
        var cmd = new nvme_admin_cmd
        {
            opcode = 0x02, // Get Log Page
            nsid = 0xFFFFFFFF,
            addr = (ulong)(&smartLog),
            data_len = (uint)sizeof(nvme_smart_log),
            cdw10 = 0x02 | ((uint)((sizeof(nvme_smart_log) / 4) - 1) << 16)
        };

        if (ioctl(fd, NVME_IOCTL_ADMIN_CMD, ref cmd) < 0)
        {
            LastUpdate = false;
            return false;
        }

        CriticalWarning = smartLog.critical_warning;
        Temperature = KelvinToCelsius((ushort)(smartLog.temperature[0] | (smartLog.temperature[1] << 8)));
        AvailableSpare = smartLog.avail_spare;
        AvailableSpareThreshold = smartLog.spare_thresh;
        PercentageUsed = smartLog.percent_used;
        DataUnitRead = Le128ToUInt64(smartLog.data_units_read);
        DataUnitWritten = Le128ToUInt64(smartLog.data_units_written);
        HostReadCommands = Le128ToUInt64(smartLog.host_reads);
        HostWriteCommands = Le128ToUInt64(smartLog.host_writes);
        ControllerBusyTime = Le128ToUInt64(smartLog.ctrl_busy_time);
        PowerCycles = Le128ToUInt64(smartLog.power_cycles);
        PowerOnHours = Le128ToUInt64(smartLog.power_on_hours);
        UnsafeShutdowns = Le128ToUInt64(smartLog.unsafe_shutdowns);
        MediaErrors = Le128ToUInt64(smartLog.media_errors);
        ErrorInfoLogEntries = Le128ToUInt64(smartLog.num_err_log_entries);
        WarningCompositeTemperatureTime = smartLog.warning_temp_time;
        CriticalCompositeTemperatureTime = smartLog.critical_comp_time;

        for (var i = 0; i < TemperatureSensors.Length; i++)
        {
            TemperatureSensors[i] = KelvinToCelsius(smartLog.temp_sensor[i]);
        }

        LastUpdate = true;
        return true;
    }

    private static unsafe ulong Le128ToUInt64(byte* p)
    {
        var v = 0ul;
        for (var i = 7; i >= 0; i--)
        {
            v = (v << 8) | p[i];
        }
        return v;
    }
}
