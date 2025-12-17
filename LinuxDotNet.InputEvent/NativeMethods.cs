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
    // TODO

    //------------------------------------------------------------------------
    // Const
    //------------------------------------------------------------------------

    public const short POLLIN = 0x0001;

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

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct PollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InputEvent
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
    public static extern int poll(ref PollFd fds, uint nfds, int timeout);
}
