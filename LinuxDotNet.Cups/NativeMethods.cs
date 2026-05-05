namespace LinuxDotNet.Cups;

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
    private const string LibCups = "libcups.so.2";

    //------------------------------------------------------------------------
    // const
    //------------------------------------------------------------------------

    public const int IPP_OP_GET_PRINTER_ATTRIBUTES = 0x000B;

    public const int IPP_TAG_OPERATION = 0x01;
    public const int IPP_TAG_URI = 0x45;
    public const int IPP_TAG_KEYWORD = 0x44;

    public const int IPP_RES_PER_INCH = 3;
    public const int IPP_RES_PER_CM = 4;

    public const int HTTP_CONTINUE = 100;

    public const int HTTP_ENCRYPTION_IF_REQUESTED = 0;
    public const int HTTP_ENCRYPTION_NEVER = 1;
    public const int HTTP_ENCRYPTION_REQUIRED = 2;

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

    public enum IppTag
    {
        IPP_TAG_ZERO = 0x00,
        IPP_TAG_OPERATION = 0x01,
        IPP_TAG_JOB = 0x02,
        IPP_TAG_END = 0x03,
        IPP_TAG_PRINTER = 0x04,
        IPP_TAG_UNSUPPORTED_GROUP = 0x05,
        IPP_TAG_SUBSCRIPTION = 0x06,
        IPP_TAG_EVENT_NOTIFICATION = 0x07,
        IPP_TAG_INTEGER = 0x21,
        IPP_TAG_BOOLEAN = 0x22,
        IPP_TAG_ENUM = 0x23,
        IPP_TAG_STRING = 0x30,
        IPP_TAG_DATE = 0x31,
        IPP_TAG_RESOLUTION = 0x32,
        IPP_TAG_RANGE = 0x33,
        IPP_TAG_BEGIN_COLLECTION = 0x34,
        IPP_TAG_TEXTLANG = 0x35,
        IPP_TAG_NAMELANG = 0x36,
        IPP_TAG_END_COLLECTION = 0x37,
        IPP_TAG_TEXT = 0x41,
        IPP_TAG_NAME = 0x42,
        IPP_TAG_KEYWORD = 0x44,
        IPP_TAG_URI = 0x45,
        IPP_TAG_URISCHEME = 0x46,
        IPP_TAG_CHARSET = 0x47,
        IPP_TAG_LANGUAGE = 0x48,
        IPP_TAG_MIMETYPE = 0x49
    }

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct cups_dest_t
    {
        public IntPtr name;
        public IntPtr instance;
        public int is_default;
        public int num_options;
        public IntPtr options;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct cups_option_t
    {
        public IntPtr name;
        public IntPtr value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct cups_job_t
    {
        public int id;
        public IntPtr dest;
        public IntPtr title;
        public IntPtr user;
        public IntPtr format;
        public int state;
        public int size;
        public int priority;
        public long completed_time;
        public long creation_time;
        public long processing_time;
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    [LibraryImport(LibCups)]
    public static partial IppStatus cupsLastError();

    [LibraryImport(LibCups)]
    public static partial IntPtr cupsLastErrorString();

    [LibraryImport(LibCups)]
    public static partial IntPtr cupsGetDefault();

    [LibraryImport(LibCups)]
    public static partial int cupsGetDests(out IntPtr dests);

    [LibraryImport(LibCups)]
    public static partial void cupsFreeDests(int num_dests, IntPtr dests);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int cupsPrintFile(
        string printer,
        string filename,
        string title,
        int num_options,
        IntPtr options);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr httpConnectEncrypt(
        string host,
        int port,
        int encryption);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int cupsCancelJob(
        string printer,
        int job_id);

    [LibraryImport(LibCups)]
    public static partial void httpClose(IntPtr http);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int cupsAddOption(
        string name,
        string value,
        int num_options,
        ref IntPtr options);

    [LibraryImport(LibCups)]
    public static partial void cupsFreeOptions(int num_options, IntPtr options);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int cupsCreateJob(
        IntPtr http,
        string printer,
        string title,
        int num_options,
        IntPtr options);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int cupsStartDocument(
        IntPtr http,
        string printer,
        int job_id,
        string docname,
        string format,
        int last_document);

    [LibraryImport(LibCups)]
    public static partial int cupsWriteRequestData(
        IntPtr http,
        byte[] buffer,
        int length);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int cupsFinishDocument(
        IntPtr http,
        string printer);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int cupsGetJobs(
        ref IntPtr jobs,
        string name,
        int myjobs,
        int whichjobs);

    [LibraryImport(LibCups)]
    public static partial void cupsFreeJobs(int num_jobs, IntPtr jobs);

    [LibraryImport(LibCups)]
    public static partial IntPtr ippNewRequest(int op);

    [LibraryImport(LibCups)]
    public static partial void ippDelete(IntPtr ipp);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr ippAddString(
        IntPtr ipp,
        int group,
        int value_tag,
        string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? language,
        string value);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cupsDoRequest(
        IntPtr http,
        IntPtr request,
        string resource);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr ippFindAttribute(
        IntPtr ipp,
        string name,
        int type);

    [LibraryImport(LibCups)]
    public static partial IntPtr ippGetString(
        IntPtr attr,
        int element,
        IntPtr language);

    [LibraryImport(LibCups)]
    public static partial int ippGetInteger(IntPtr attr, int element);

    [LibraryImport(LibCups)]
    public static partial int ippGetBoolean(IntPtr attr, int element);

    [LibraryImport(LibCups)]
    public static partial int ippGetCount(IntPtr attr);

    [LibraryImport(LibCups)]
    public static partial IntPtr ippFirstAttribute(IntPtr ipp);

    [LibraryImport(LibCups)]
    public static partial IntPtr ippNextAttribute(IntPtr ipp);

    [LibraryImport(LibCups)]
    public static partial IntPtr ippGetName(IntPtr attr);

    [LibraryImport(LibCups)]
    public static partial int ippGetValueTag(IntPtr attr);

    [LibraryImport(LibCups)]
    public static partial int ippGetResolution(
        IntPtr attr,
        int element,
        out int xres,
        out int yres,
        out int units);
}
