namespace LinuxDotNet.Disk;

using System.Runtime.InteropServices;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA2101
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static class NativeMethods
{
    //------------------------------------------------------------------------
    // const
    //------------------------------------------------------------------------

    // Constants
    public const int O_RDONLY = 0;
    public const int O_NONBLOCK = 0x800;
    public const int SG_DXFER_FROM_DEV = -3;
    public const int SG_INFO_OK_MASK = 0x1;
    public const int SG_INFO_OK = 0x0;
    public const int EIO = 5;

    // SG_IO constant (0x2285 on most Linux systems)
    public const ulong SG_IO = 0x2285;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    // sg_io_hdr_t structure
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

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [DllImport("libc", SetLastError = true)]
    public static extern int open([MarshalAs(UnmanagedType.LPStr)] string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    public static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    public static extern unsafe int ioctl(int fd, ulong request, void* argp);
}
