namespace LinuxDotNet.Video4Linux2;

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
    // Const
    //------------------------------------------------------------------------

    // TODO 不要削除、値再確認

    // ioctl
    private const uint IOC_NRBITS = 8;
    private const uint IOC_TYPEBITS = 8;
    private const uint IOC_SIZEBITS = 14;

    private const uint IOC_NRSHIFT = 0;
    private const uint IOC_TYPESHIFT = IOC_NRSHIFT + IOC_NRBITS;
    private const uint IOC_SIZESHIFT = IOC_TYPESHIFT + IOC_TYPEBITS;
    private const uint IOC_DIRSHIFT = IOC_SIZESHIFT + IOC_SIZEBITS;

    private const uint IOC_WRITE = 1;
    private const uint IOC_READ = 2;

    public static readonly uint VIDIOC_QUERYCAP;
    public static readonly uint VIDIOC_G_FMT;
    public static readonly uint VIDIOC_S_FMT;
    public static readonly uint VIDIOC_REQBUFS;
    public static readonly uint VIDIOC_QUERYBUF;
    public static readonly uint VIDIOC_QBUF;
    public static readonly uint VIDIOC_DQBUF;
    public static readonly uint VIDIOC_STREAMON;
    public static readonly uint VIDIOC_STREAMOFF;
    public static readonly uint VIDIOC_ENUM_FMT;
    public static readonly uint VIDIOC_ENUM_FRAMESIZES;

    // open
    public const int O_RDWR = 2;
    public const int O_NONBLOCK = 0x800;

    // mmap
    public const int PROT_READ = 0x1;
    public const int PROT_WRITE = 0x2;
    public const int MAP_SHARED = 0x01;

    // V4L2
    public const uint V4L2_BUF_TYPE_VIDEO_CAPTURE = 1;
    public const uint V4L2_MEMORY_MMAP = 1;
    public const uint V4L2_FIELD_NONE = 1;

    // PixelFormat
    public const uint V4L2_PIX_FMT_YUYV = 0x56595559;
    public const uint V4L2_PIX_FMT_MJPEG = 0x47504a4d;

    // Frame size
    public const uint V4L2_FRMSIZE_TYPE_DISCRETE = 1;
    public const uint V4L2_FRMSIZE_TYPE_CONTINUOUS = 2;
    public const uint V4L2_FRMSIZE_TYPE_STEPWISE = 3;

    // Capability
    public const uint V4L2_CAP_VIDEO_CAPTURE = 0x00000001;
    public const uint V4L2_CAP_VIDEO_OUTPUT = 0x00000002;
    public const uint V4L2_CAP_VIDEO_OVERLAY = 0x00000004;
    public const uint V4L2_CAP_VBI_CAPTURE = 0x00000010;
    public const uint V4L2_CAP_VBI_OUTPUT = 0x00000020;
    public const uint V4L2_CAP_SLICED_VBI_CAPTURE = 0x00000040;
    public const uint V4L2_CAP_SLICED_VBI_OUTPUT = 0x00000080;
    public const uint V4L2_CAP_RDS_CAPTURE = 0x00000100;
    public const uint V4L2_CAP_VIDEO_OUTPUT_OVERLAY = 0x00000200;
    public const uint V4L2_CAP_HW_FREQ_SEEK = 0x00000400;
    public const uint V4L2_CAP_RDS_OUTPUT = 0x00000800;
    public const uint V4L2_CAP_VIDEO_CAPTURE_MPLANE = 0x00001000;
    public const uint V4L2_CAP_VIDEO_OUTPUT_MPLANE = 0x00002000;
    public const uint V4L2_CAP_VIDEO_M2M_MPLANE = 0x00004000;
    public const uint V4L2_CAP_VIDEO_M2M = 0x00008000;
    public const uint V4L2_CAP_TUNER = 0x00010000;
    public const uint V4L2_CAP_AUDIO = 0x00020000;
    public const uint V4L2_CAP_RADIO = 0x00040000;
    public const uint V4L2_CAP_MODULATOR = 0x00080000;
    public const uint V4L2_CAP_SDR_CAPTURE = 0x00100000;
    public const uint V4L2_CAP_EXT_PIX_FORMAT = 0x00200000;
    public const uint V4L2_CAP_SDR_OUTPUT = 0x00400000;
    public const uint V4L2_CAP_META_CAPTURE = 0x00800000;
    public const uint V4L2_CAP_READWRITE = 0x01000000;
    public const uint V4L2_CAP_ASYNCIO = 0x02000000;
    public const uint V4L2_CAP_STREAMING = 0x04000000;
    public const uint V4L2_CAP_META_OUTPUT = 0x08000000;
    public const uint V4L2_CAP_TOUCH = 0x10000000;
    public const uint V4L2_CAP_DEVICE_CAPS = 0x80000000;

    // poll
    public const short POLLIN = 0x0001;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public unsafe struct v4l2_capability
    {
        public const int DriverSize = 16;
        public const int CardSize = 32;
        public const int BusInfoSize = 32;

        public fixed byte driver[DriverSize];
        public fixed byte card[CardSize];
        public fixed byte bus_info[BusInfoSize];
        public uint version;
        public uint capabilities;
        public fixed uint reserved[4];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct v4l2_pix_format
    {
        public uint width;
        public uint height;
        public uint pixelformat;
        public uint field;
        public uint bytesperline;
        public uint sizeimage;
        public uint colorspace;
        public uint priv;
        public uint flags;
        public uint ycbcr_enc;
        public uint quantization;
        public uint xfer_func;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 8)]
    public unsafe struct v4l2_format_fmt
    {
        [FieldOffset(0)]
        public v4l2_pix_format pix;

        [FieldOffset(0)]
        public fixed byte raw_data[200];

        // alignment purpose
        [FieldOffset(0)]
        private long align;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct v4l2_format
    {
        public uint type;
        public v4l2_format_fmt fmt;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public unsafe struct v4l2_requestbuffers
    {
        public uint count;
        public uint type;
        public uint memory;
        public fixed uint reserved[2];
    }

    [StructLayout(LayoutKind.Explicit, Pack = 8)]
    public struct v4l2_buffer_m
    {
        [FieldOffset(0)]
        public uint offset;

        [FieldOffset(0)]
        public nuint userptr;

        [FieldOffset(0)]
        public int fd;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public unsafe struct v4l2_buffer
    {
        public uint index;
        public uint type;
        public uint bytesused;
        public uint flags;
        public uint field;

        // struct timeval
        public nint tv_sec;
        public nint tv_usec;

        // struct v4l2_timecode
        public uint timecode_type;
        public uint timecode_flags;
        public byte timecode_frames;
        public byte timecode_seconds;
        public byte timecode_minutes;
        public byte timecode_hours;
        public fixed byte timecode_userbits[4];

        public uint sequence;
        public uint memory;

        // union m
        public v4l2_buffer_m m;

        public uint length;
        public uint reserved2;
        public uint reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public unsafe struct v4l2_fmtdesc
    {
        public const int DescriptionSize = 32;

        public uint index;
        public uint type;
        public uint flags;
        public fixed byte description[DescriptionSize];
        public uint pixelformat;
        public fixed uint reserved[4];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct v4l2_frmsize_discrete
    {
        public uint width;
        public uint height;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct v4l2_frmsize_stepwise
    {
        public uint min_width;
        public uint max_width;
        public uint step_width;
        public uint min_height;
        public uint max_height;
        public uint step_height;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 8)]
    public struct v4l2_frmsizeenum_union
    {
        [FieldOffset(0)]
        public v4l2_frmsize_discrete discrete;
        [FieldOffset(0)]
        public v4l2_frmsize_stepwise stepwise;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public unsafe struct v4l2_frmsizeenum
    {
        public uint index;
        public uint pixel_format;
        public uint type;
        public v4l2_frmsizeenum_union size;
        public fixed uint reserved[2];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct pollfd
    {
        public int fd;
        public short events;
        public short revents;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [DllImport("libc", SetLastError = true)]
    public static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    public static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    public static extern int ioctl(int fd, uint request, IntPtr argp);

    [DllImport("libc", SetLastError = true)]
    public static extern IntPtr mmap(IntPtr addr, int length, int prot, int flags, int fd, int offset);

    [DllImport("libc", SetLastError = true)]
    public static extern int munmap(IntPtr addr, int length);

    [DllImport("libc", SetLastError = true)]
    public static extern unsafe int poll(ref pollfd fds, uint nfds, int timeout);

    //------------------------------------------------------------------------
    // Initialize
    //------------------------------------------------------------------------

    private static uint IOC(uint dir, uint type, uint nr, int size) =>
        (dir << (int)IOC_DIRSHIFT) | (type << (int)IOC_TYPESHIFT) | (nr << (int)IOC_NRSHIFT) | ((uint)size << (int)IOC_SIZESHIFT);

    private static uint IOR(char type, uint nr, int size) => IOC(IOC_READ, (byte)type, nr, size);

    private static uint IOW(char type, uint nr, int size) => IOC(IOC_WRITE, (byte)type, nr, size);

    private static uint IOWR(char type, uint nr, int size) => IOC(IOC_READ | IOC_WRITE, (byte)type, nr, size);

#pragma warning disable CA1810
    static unsafe NativeMethods()
    {
        VIDIOC_QUERYCAP = IOR('V', 0, sizeof(v4l2_capability));
        VIDIOC_G_FMT = IOWR('V', 4, sizeof(v4l2_format));
        VIDIOC_S_FMT = IOWR('V', 5, sizeof(v4l2_format));
        VIDIOC_REQBUFS = IOWR('V', 8, sizeof(v4l2_requestbuffers));
        VIDIOC_QUERYBUF = IOWR('V', 9, sizeof(v4l2_buffer));
        VIDIOC_QBUF = IOWR('V', 15, sizeof(v4l2_buffer));
        VIDIOC_DQBUF = IOWR('V', 17, sizeof(v4l2_buffer));
        VIDIOC_STREAMON = IOW('V', 18, sizeof(int));
        VIDIOC_STREAMOFF = IOW('V', 19, sizeof(int));
        VIDIOC_ENUM_FMT = IOWR('V', 2, sizeof(v4l2_fmtdesc));
        VIDIOC_ENUM_FRAMESIZES = IOWR('V', 74, sizeof(v4l2_frmsizeenum));
    }
#pragma warning restore CA1810
}
