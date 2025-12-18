namespace LinuxDotNet.InputEvent;

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

    public const short POLLIN = 0x0001;
    public const short POLLPRI = 0x0002;
    public const short POLLOUT = 0x0004;
    public const short POLLERR = 0x0008;
    public const short POLLHUP = 0x0010;
    public const short POLLNVAL = 0x0020;

    // Event type
    public const ushort EV_SYN = 0x00;
    public const ushort EV_KEY = 0x01;
    public const ushort EV_REL = 0x02;
    public const ushort EV_ABS = 0x03;
    public const ushort EV_MSC = 0x04;

    // Key state
    public const int EV_RELEASED = 0;
    public const int EV_PRESSED = 1;
    public const int EV_REPEAT = 2;

    // Ioctl
    public const uint EVIOCGNAME = 0x80ff4506;
    public const uint EVIOCGRAB = 0x40044590;

    // Error
    public const int EINTR = 4;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct pollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct input_event
    {
        public long tv_sec;      // seconds
        public long tv_usec;     // microseconds
        public ushort type;
        public ushort code;
        public int value;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [DllImport("libc", SetLastError = true)]
    public static extern unsafe int ioctl(int fd, uint request, void* argp);

    [DllImport("libc", SetLastError = true)]
    public static extern int ioctl(int fd, uint request, int arg);

    [DllImport("libc", SetLastError = true)]
    public static extern int poll(ref pollFd fds, uint nfds, int timeout);
}
