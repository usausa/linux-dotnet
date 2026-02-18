namespace LinuxDotNet.SystemInfo;

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

    public const short SC_CLK_TCK = 2;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct statfs
    {
        public ulong f_type;
        public ulong f_bsize;
        public ulong f_blocks;
        public ulong f_bfree;
        public ulong f_bavail;
        public ulong f_files;
        public ulong f_ffree;
        public long f_fsid1;
        public long f_fsid2;
        public ulong f_namelen;
        public ulong f_frsize;
        public ulong f_flags;
        public ulong f_spare1;
        public ulong f_spare2;
        public ulong f_spare3;
        public ulong f_spare4;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [DllImport("libc", SetLastError = true)]
    public static extern long sysconf(int name);

    [DllImport("libc", SetLastError = true)]
    public static extern int statfs64([MarshalAs(UnmanagedType.LPStr)] string path, ref statfs buf);
}
