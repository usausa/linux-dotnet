namespace LinuxDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static LinuxDotNet.SystemInfo.NativeMethods;

internal sealed class SafeDirectoryHandle : SafeHandle
{
    public SafeDirectoryHandle(IntPtr dir)
        : base(IntPtr.Zero, true)
    {
        SetHandle(dir);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        _ = closedir(handle);
        return true;
    }
}
