namespace LinuxDotNet.SystemInfo;

using System.Runtime.InteropServices;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static partial class NativeMethods
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
        public int f_fsid1;
        public int f_fsid2;
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

    [LibraryImport("libc", SetLastError = true)]
    public static partial long sysconf(int name);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int statfs64(string path, ref statfs buf);
}
