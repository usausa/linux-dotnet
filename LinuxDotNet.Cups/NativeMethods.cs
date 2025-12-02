namespace LinuxDotNet.Cups;

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
    private const string LibCups = "libcups.so.2";

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct cups_dest_t
    {
        public IntPtr name;
        public IntPtr instance;
        public int isDefault;
        public int numOptions;
        public IntPtr options;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr cupsGetDefault();

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr cupsGetDests(out int num_dests);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern void cupsFreeDests(int num_dests, IntPtr dests);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsPrintFile(string printer, string filename, string title, int numOptions, IntPtr options);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr cupsLastErrorString();
}
