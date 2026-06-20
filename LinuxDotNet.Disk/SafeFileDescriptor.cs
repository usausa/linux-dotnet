namespace LinuxDotNet.Disk;

using System.Runtime.InteropServices;

using static LinuxDotNet.Disk.NativeMethods;

internal sealed class SafeFileDescriptor : SafeHandle
{
    public SafeFileDescriptor(int fd)
        : base(new IntPtr(-1), true)
    {
        SetHandle(new IntPtr(fd));
    }

    public override bool IsInvalid => handle == new IntPtr(-1);

    public int Descriptor => (int)handle;

    protected override bool ReleaseHandle()
    {
        _ = close((int)handle);
        return true;
    }
}
