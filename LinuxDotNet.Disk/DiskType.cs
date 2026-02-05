namespace LinuxDotNet.Disk;

public enum DiskType
{
    Unknown = 0,
    // Major 8: SCSI subsystem (includes SATA, SAS, USB storage via sd* driver)
    Scsi,
    // Major 3, 22: Legacy IDE block devices
    Ide,
    // Major 259: NVMe block devices
    Nvme,
    // Major 179: MMC/SD card block devices
    Mmc,
    // Major 252: Virtual I/O block devices (virtual machines)
    Virtual
}
