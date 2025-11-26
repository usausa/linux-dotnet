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

    // ioctl
    public const uint VIDIOC_QUERYCAP = 0x80685600;
    public const uint VIDIOC_S_FMT = 0xc0d05605;
    public const uint VIDIOC_G_FMT = 0xc0cc5604;
    public const uint VIDIOC_REQBUFS = 0xc0145608;
    public const uint VIDIOC_QUERYBUF = 0xc0445609;
    public const uint VIDIOC_QBUF = 0xc044560f;
    public const uint VIDIOC_DQBUF = 0xc0445611;
    public const uint VIDIOC_STREAMON = 0x40045612;
    public const uint VIDIOC_STREAMOFF = 0x40045613;
    public const uint VIDIOC_ENUM_FMT = 0xc0405602;
    public const uint VIDIOC_ENUM_FRAMESIZES = 0xc02c564a;
    public const uint VIDIOC_ENUM_FRAMEINTERVALS = 0xc034564b;

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

    // Fmame size
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

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_capability
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] driver;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] card;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] bus_info;
        public uint version;
        public uint capabilities;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
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
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_format
    {
        public uint type;
        public v4l2_pix_format fmt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_requestbuffers
    {
        public uint count;
        public uint type;
        public uint memory;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_buffer
    {
        public uint index;
        public uint type;
        public uint bytesused;
        public uint flags;
        public uint field;
        public timeval timestamp;
        public v4l2_timecode timecode;
        public uint sequence;
        public uint memory;
        public uint offset;
        public IntPtr userptr;
        public uint length;
        public uint reserved2;
        public uint reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct timeval
    {
        public IntPtr tv_sec;
        public IntPtr tv_usec;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_timecode
    {
        public uint type;
        public uint flags;
        public byte frames;
        public byte seconds;
        public byte minutes;
        public byte hours;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] userbits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_fmtdesc
    {
        public uint index;
        public uint type;
        public uint flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] description;
        public uint pixelformat;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_frmsize_discrete
    {
        public uint width;
        public uint height;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_frmsize_stepwise
    {
        public uint min_width;
        public uint max_width;
        public uint step_width;
        public uint min_height;
        public uint max_height;
        public uint step_height;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct v4l2_frmsizeenum_union
    {
        [FieldOffset(0)]
        public v4l2_frmsize_discrete discrete;

        [FieldOffset(0)]
        public v4l2_frmsize_stepwise stepwise;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_frmsizeenum
    {
        public uint index;
        public uint pixel_format;
        public uint type;
        public v4l2_frmsizeenum_union size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] reserved;
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
}
