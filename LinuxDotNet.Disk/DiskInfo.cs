namespace LinuxDotNet.Disk;

using System.Text.RegularExpressions;

using static LinuxDotNet.Disk.Helper;
using static LinuxDotNet.Disk.NativeMethods;

public static partial class DiskInfo
{
    private const string SysBlockPath = "/sys/block";

    public static IReadOnlyList<IDiskInfo> GetInformation()
    {
        var list = new List<IDiskInfo>();

        var directories = Directory.GetDirectories(SysBlockPath)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();

        var index = 0u;
        foreach (var deviceName in directories)
        {
            if (!IsPhysicalDisk(deviceName))
            {
                continue;
            }

            var major = GetDeviceMajorNumber(deviceName);
            if (major == -1)
            {
                continue;
            }

            var devicePath = $"/dev/{deviceName}";

            var info = new DiskInfoGeneric
            {
                Index = index++,
                DeviceName = devicePath,
                DiskType = GetDiskTypeFromMajor(major),
                Removable = ReadFileAsBool(Path.Combine(SysBlockPath, deviceName, "removable")) ?? false
            };

            // Get size information
            var sectors = ReadFileAsUInt64(Path.Combine(SysBlockPath, deviceName, "size"));
            var logicalBlockSize = ReadFileAsUInt32(Path.Combine(SysBlockPath, deviceName, "queue", "logical_block_size")) ?? 512;
            var physicalBlockSize = ReadFileAsUInt32(Path.Combine(SysBlockPath, deviceName, "queue", "physical_block_size")) ?? 512;

            info.TotalSectors = sectors ?? 0;
            info.LogicalBlockSize = logicalBlockSize;
            info.PhysicalBlockSize = physicalBlockSize;
            info.Size = (sectors ?? 0) * logicalBlockSize;

            // Get device-specific information
            if (major == NVME_MAJOR)
            {
                ReadNvmeInfo(deviceName, info);
                info.SmartType = SmartType.Nvme;
                info.Smart = new SmartNvme(devicePath);
                info.Smart.Update();
            }
            else if (major == MMC_BLOCK_MAJOR)
            {
                ReadMmcInfo(deviceName, info);
                info.SmartType = SmartType.Unsupported;
                info.Smart = SmartUnsupported.Default;
            }
            else if (major is SCSI_DISK0_MAJOR or IDE0_MAJOR or IDE1_MAJOR)
            {
                ReadIdeInfo(deviceName, info);
                var smart = new SmartGeneric(devicePath);
                if (smart.Update())
                {
                    info.SmartType = SmartType.Generic;
                    info.Smart = smart;
                }
                else
                {
                    smart.Dispose();
                    info.SmartType = SmartType.Unsupported;
                    info.Smart = SmartUnsupported.Default;
                }
            }
            else
            {
                ReadIdeInfo(deviceName, info);
                info.SmartType = SmartType.Unsupported;
                info.Smart = SmartUnsupported.Default;
            }

            list.Add(info);
        }

        return list;
    }

    [GeneratedRegex(@"nvme\d+n\d+p\d+")]
    private static partial Regex NvmeDeviceRegex();

    [GeneratedRegex(@"^[sh]d[a-z]+\d+$")]
    private static partial Regex IdeDeviceRegex();

    [GeneratedRegex(@"^vd[a-z]+\d+$")]
    private static partial Regex VirtualDeviceRegex();

    [GeneratedRegex(@"^mmcblk\d+p\d+$")]
    private static partial Regex MmcDeviceRegex();

    private static bool IsPhysicalDisk(string deviceName)
    {
        // Skip loop, ram and dm devices
        if (deviceName.StartsWith("loop", StringComparison.Ordinal) ||
            deviceName.StartsWith("ram", StringComparison.Ordinal) ||
            deviceName.StartsWith("dm-", StringComparison.Ordinal))
        {
            return false;
        }

        // NVMe: nvme0n1 is disk, nvme0n1p1 is partition
        if (deviceName.StartsWith("nvme", StringComparison.Ordinal))
        {
            return !NvmeDeviceRegex().IsMatch(deviceName);
        }

        // SATA/SCSI: sda is disk, sda1 is partition
        if (deviceName.StartsWith("sd", StringComparison.Ordinal) ||
            deviceName.StartsWith("hd", StringComparison.Ordinal))
        {
            return !IdeDeviceRegex().IsMatch(deviceName);
        }

        // Virtual I/O: vda is disk, vda1 is partition
        if (deviceName.StartsWith("vd", StringComparison.Ordinal))
        {
            return !VirtualDeviceRegex().IsMatch(deviceName);
        }

        // MMC: mmcblk0 is disk, mmcblk0p1 is partition
        if (deviceName.StartsWith("mmcblk", StringComparison.Ordinal))
        {
            return !MmcDeviceRegex().IsMatch(deviceName);
        }

        return false;
    }

    private static int GetDeviceMajorNumber(string deviceName)
    {
        var devPath = Path.Combine(SysBlockPath, deviceName, "dev");
        var str = ReadFile(devPath);
        if (str is null)
        {
            return -1;
        }

        var span = str.AsSpan();
        var index = span.IndexOf(':');
        if (index < 0)
        {
            return -1;
        }

        return Int32.TryParse(span[..index], out var major) ? major : -1;
    }

    private static DiskType GetDiskTypeFromMajor(int major)
    {
        return major switch
        {
            NVME_MAJOR => DiskType.Nvme,
            SCSI_DISK0_MAJOR => DiskType.Scsi,
            IDE0_MAJOR or IDE1_MAJOR => DiskType.Ide,
            MMC_BLOCK_MAJOR => DiskType.Mmc,
            VIRTBLK_MAJOR => DiskType.Virtual,
            _ => DiskType.Unknown
        };
    }

    private static void ReadNvmeInfo(string deviceName, DiskInfoGeneric info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");

        info.Model = ReadFile(Path.Combine(devicePath, "model")) ?? string.Empty;
        info.SerialNumber = ReadFile(Path.Combine(devicePath, "serial")) ?? string.Empty;
        info.FirmwareRevision = ReadFile(Path.Combine(devicePath, "firmware_rev")) ?? string.Empty;
    }

    private static void ReadIdeInfo(string deviceName, DiskInfoGeneric info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");
        var vendor = ReadFile(Path.Combine(devicePath, "vendor"));
        var model = ReadFile(Path.Combine(devicePath, "model"));

        if (!String.IsNullOrEmpty(vendor) && !String.IsNullOrEmpty(model))
        {
            info.Model = $"{vendor} {model}";
        }
        else if (!String.IsNullOrEmpty(model))
        {
            info.Model = model;
        }
        else if (!String.IsNullOrEmpty(vendor))
        {
            info.Model = vendor;
        }
        else
        {
            info.Model = string.Empty;
        }

        info.FirmwareRevision = ReadFile(Path.Combine(devicePath, "rev")) ?? string.Empty;
        info.SerialNumber = string.Empty; // SCSI serial usually not available in sysfs
    }

    private static void ReadMmcInfo(string deviceName, DiskInfoGeneric info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");
        var name = ReadFile(Path.Combine(devicePath, "name"));
        var type = ReadFile(Path.Combine(devicePath, "type"));

        if (!String.IsNullOrEmpty(type) && !String.IsNullOrEmpty(name))
        {
            info.Model = $"{type} {name}";
        }
        else if (!String.IsNullOrEmpty(name))
        {
            info.Model = name;
        }
        else
        {
            info.Model = string.Empty;
        }

        info.SerialNumber = ReadFile(Path.Combine(devicePath, "cid")) ?? string.Empty;
        info.FirmwareRevision = ReadFile(Path.Combine(devicePath, "fwrev")) ?? string.Empty;
    }
}
