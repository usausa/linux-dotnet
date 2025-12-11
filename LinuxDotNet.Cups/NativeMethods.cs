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
    // Enum
    //------------------------------------------------------------------------

    public enum IppStatus
    {
        IPP_OK = 0x0000,
        IPP_OK_SUBST = 0x0001,
        IPP_OK_CONFLICT = 0x0002,
        IPP_OK_IGNORED_SUBSCRIPTIONS = 0x0003,
        IPP_OK_IGNORED_NOTIFICATIONS = 0x0004,
        IPP_OK_TOO_MANY_EVENTS = 0x0005,
        IPP_OK_BUT_CANCEL_SUBSCRIPTION = 0x0006,
        IPP_REDIRECTION_OTHER_SITE = 0x0200,
        IPP_BAD_REQUEST = 0x0400,
        IPP_FORBIDDEN = 0x0401,
        IPP_NOT_AUTHENTICATED = 0x0402,
        IPP_NOT_AUTHORIZED = 0x0403,
        IPP_NOT_POSSIBLE = 0x0404,
        IPP_TIMEOUT = 0x0405,
        IPP_NOT_FOUND = 0x0406,
        IPP_GONE = 0x0407,
        IPP_REQUEST_ENTITY = 0x0408,
        IPP_REQUEST_VALUE = 0x0409,
        IPP_DOCUMENT_FORMAT = 0x040a,
        IPP_ATTRIBUTES = 0x040b,
        IPP_URI_SCHEME = 0x040c,
        IPP_CHARSET = 0x040d,
        IPP_CONFLICT = 0x040e,
        IPP_COMPRESSION_NOT_SUPPORTED = 0x040f,
        IPP_COMPRESSION_ERROR = 0x0410,
        IPP_DOCUMENT_FORMAT_ERROR = 0x0411,
        IPP_DOCUMENT_ACCESS_ERROR = 0x0412,
        IPP_INTERNAL_ERROR = 0x0500,
        IPP_OPERATION_NOT_SUPPORTED = 0x0501,
        IPP_SERVICE_UNAVAILABLE = 0x0502,
        IPP_VERSION_NOT_SUPPORTED = 0x0503,
        IPP_DEVICE_ERROR = 0x0504,
        IPP_TEMPORARY_ERROR = 0x0505,
        IPP_NOT_ACCEPTING = 0x0506,
        IPP_PRINTER_BUSY = 0x0507,
        IPP_ERROR_JOB_CANCELED = 0x0508,
        IPP_MULTIPLE_JOBS_NOT_SUPPORTED = 0x0509
    }

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

    [StructLayout(LayoutKind.Sequential)]
    public struct cups_option_t
    {
        public IntPtr name;
        public IntPtr value;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr cupsGetDefault();

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsGetDests(out IntPtr dests);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern void cupsFreeDests(int num_dests, IntPtr dests);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsPrintFile(
        [MarshalAs(UnmanagedType.LPStr)] string printer,
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        [MarshalAs(UnmanagedType.LPStr)] string title,
        int num_options,
        IntPtr options);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr httpConnectEncrypt(
        [MarshalAs(UnmanagedType.LPStr)] string host,
        int port,
        int encryption);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsCancelJob(
        [MarshalAs(UnmanagedType.LPStr)] string printer,
        int job_id);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern void httpClose(IntPtr http);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsAddOption(
        [MarshalAs(UnmanagedType.LPStr)] string name,
        [MarshalAs(UnmanagedType.LPStr)] string value,
        int num_options,
        ref IntPtr options);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern void cupsFreeOptions(int num_options, IntPtr options);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsCreateJob(
        IntPtr http,
        [MarshalAs(UnmanagedType.LPStr)] string printer,
        [MarshalAs(UnmanagedType.LPStr)] string title,
        int num_options,
        IntPtr options);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsStartDocument(
        IntPtr http,
        [MarshalAs(UnmanagedType.LPStr)] string printer,
        int job_id,
        [MarshalAs(UnmanagedType.LPStr)] string docname,
        [MarshalAs(UnmanagedType.LPStr)] string format,
        int last_document);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsWriteRequestData(
        IntPtr http,
        byte[] buffer,
        int length);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cupsFinishDocument(
        IntPtr http,
        [MarshalAs(UnmanagedType.LPStr)] string printer);

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern IppStatus cupsLastError();

    [DllImport(LibCups, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr cupsLastErrorString();
}
