namespace LinuxDotNet.Disk;

using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA2101
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static class NativeMethods
{
    //------------------------------------------------------------------------
    // Const
    //------------------------------------------------------------------------

    public const int O_RDONLY = 0;

    public const ulong SG_IO = 0x2285;
    public const ulong NVME_IOCTL_ADMIN_CMD = 0xC0484E41;

    public const int SG_DXFER_FROM_DEV = -3;

    public const uint SG_INFO_OK_MASK = 0x1;

    public const uint SG_INFO_OK = 0x0;

    // Linux major numbers

    public const int NVME_MAJOR = 259;
    public const int SCSI_DISK0_MAJOR = 8;
    public const int IDE0_MAJOR = 3;
    public const int IDE1_MAJOR = 22;
    public const int MMC_BLOCK_MAJOR = 179;
    public const int VIRTBLK_MAJOR = 252;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct sg_io_hdr_t
    {
        public int interface_id;
        public int dxfer_direction;
        public byte cmd_len;
        public byte mx_sb_len;
        public ushort iovec_count;
        public uint dxfer_len;
        public void* dxferp;
        public byte* cmdp;
        public byte* sbp;
        public uint timeout;
        public uint flags;
        public int pack_id;
        public void* usr_ptr;
        public byte status;
        public byte masked_status;
        public byte msg_status;
        public byte sb_len_wr;
        public ushort host_status;
        public ushort driver_status;
        public int resid;
        public uint duration;
        public uint info;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct nvme_admin_cmd
    {
        public byte opcode;
        public byte flags;
        public ushort rsvd1;
        public uint nsid;
        public uint cdw2;
        public uint cdw3;
        public ulong metadata;
        public ulong addr;
        public uint metadata_len;
        public uint data_len;
        public uint cdw10;
        public uint cdw11;
        public uint cdw12;
        public uint cdw13;
        public uint cdw14;
        public uint cdw15;
        public uint timeout_ms;
        public uint result;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct nvme_smart_log
    {
        public byte critical_warning;
        public fixed byte temperature[2];
        public byte avail_spare;
        public byte spare_thresh;
        public byte percent_used;
        public fixed byte rsvd6[26];
        public fixed byte data_units_read[16];
        public fixed byte data_units_written[16];
        public fixed byte host_reads[16];
        public fixed byte host_writes[16];
        public fixed byte ctrl_busy_time[16];
        public fixed byte power_cycles[16];
        public fixed byte power_on_hours[16];
        public fixed byte unsafe_shutdowns[16];
        public fixed byte media_errors[16];
        public fixed byte num_err_log_entries[16];
        public uint warning_temp_time;
        public uint critical_comp_time;
        public fixed ushort temp_sensor[8];
        public uint thm_temp1_trans_count;
        public uint thm_temp2_trans_count;
        public uint thm_temp1_total_time;
        public uint thm_temp2_total_time;
        public fixed byte rsvd232[280];
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [DllImport("libc", SetLastError = true)]
    public static extern int open([MarshalAs(UnmanagedType.LPStr)] string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    public static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    public static extern unsafe int ioctl(int fd, ulong request, void* argp);

    [DllImport("libc", SetLastError = true)]
    public static extern int ioctl(int fd, ulong request, ref nvme_admin_cmd data);

    //------------------------------------------------------------------------
    // Helper
    //------------------------------------------------------------------------

    public static uint GetMajor(ulong dev) => (uint)((dev >> 8) & 0xfff);

    public static bool IsBlockDevice(uint mode) => (mode & 0xF000) == 0x6000;
}
#pragma warning restore CS8981
#pragma warning restore CA5392
#pragma warning restore CA2101
#pragma warning restore IDE1006
// ReSharper restore InconsistentNaming
// ReSharper restore IdentifierTypo
