namespace LinuxDotNet.Cups;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using static LinuxDotNet.Cups.NativeMethods;

[SupportedOSPlatform("linux")]
public static class CupsPrinter
{
    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string GetLastErrorMessage()
    {
        var errorPtr = cupsLastErrorString();
        return Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown error";
    }

    //--------------------------------------------------------------------------------
    // Information
    //--------------------------------------------------------------------------------

    public static string? GetDefaultPrinter()
    {
        var printerPtr = cupsGetDefault();
        if (printerPtr == IntPtr.Zero)
        {
            return null;
        }
        return Marshal.PtrToStringAnsi(printerPtr);
    }

    public static unsafe IReadOnlyList<PrinterInfo> GetPrinters()
    {
        var printers = new List<PrinterInfo>();

        var num = 0;
        var ptr = IntPtr.Zero;
        try
        {
            num = cupsGetDests(out ptr);
            if (ptr == IntPtr.Zero)
            {
                return printers;
            }

            for (var i = 0; i < num; i++)
            {
                var dest = (cups_dest_t*)IntPtr.Add(ptr, i * sizeof(cups_dest_t));
                var printer = Marshal.PtrToStringAnsi(dest->name);
                if (!String.IsNullOrEmpty(printer))
                {
                    var info = new PrinterInfo(
                        printer,
                        dest->instance != IntPtr.Zero ? Marshal.PtrToStringAnsi(dest->instance) : null,
                        dest->is_default != 0);

                    for (var j = 0; j < dest->num_options; j++)
                    {
                        var option = (cups_option_t*)IntPtr.Add(dest->options, j * sizeof(cups_option_t));
                        var name = Marshal.PtrToStringAnsi(option->name);
                        var value = Marshal.PtrToStringAnsi(option->value);

                        if ((name is not null) && (value is not null))
                        {
                            info.Options[name] = value;
                        }
                    }

                    printers.Add(info);
                }
            }
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                cupsFreeDests(num, ptr);
            }
        }

        return printers;
    }

    //--------------------------------------------------------------------------------
    // Detail
    //--------------------------------------------------------------------------------

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetPrinterAttributes(string printer)
    {
        var attributes = new Dictionary<string, IReadOnlyList<string>>();

        var http = IntPtr.Zero;
        var request = IntPtr.Zero;
        var response = IntPtr.Zero;
        try
        {
            http = httpConnectEncrypt("localhost", 631, 0);
            if (http == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to connect cups server. error=[{cupsLastError()}], message=[{GetLastErrorMessage()}]");
            }

            // Create IPP request
            request = ippNewRequest(IPP_OP_GET_PRINTER_ATTRIBUTES);
            if (request == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create ipp request. error=[{cupsLastError()}], message=[{GetLastErrorMessage()}]");
            }

            // Add request attributes
            var printerUri = $"ipp://localhost/printers/{printer}";
            ippAddString(request, IPP_TAG_OPERATION, IPP_TAG_URI, "printer-uri", null, printerUri);
            ippAddString(request, IPP_TAG_OPERATION, IPP_TAG_KEYWORD, "requested-attributes", null, "all");

            // Do request
            response = cupsDoRequest(http, request, $"/printers/{printer}");
            request = IntPtr.Zero;

            if (response == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to get ipp response. error=[{cupsLastError()}], message=[{GetLastErrorMessage()}]");
            }

            var attr = ippFirstAttribute(response);
            while (attr != IntPtr.Zero)
            {
                var namePtr = ippGetName(attr);
                if (namePtr != IntPtr.Zero)
                {
                    var attrName = Marshal.PtrToStringAnsi(namePtr);
                    if (attrName != null)
                    {
                        var valueTag = ippGetValueTag(attr);
                        var values = new List<string>();

                        if (valueTag == (int)IppTag.IPP_TAG_RESOLUTION)
                        {
                            var count = ippGetCount(attr);
                            for (var i = 0; i < count; i++)
                            {
                                if (ippGetResolution(attr, i, out var x, out var y, out var units) != 0)
                                {
                                    var unitStr = units == IPP_RES_PER_INCH ? "dpi" : "dpcm";
                                    values.Add($"{x}x{y}{unitStr}");
                                }
                            }
                        }
                        else if (valueTag == (int)IppTag.IPP_TAG_INTEGER || valueTag == (int)IppTag.IPP_TAG_ENUM)
                        {
                            var count = ippGetCount(attr);
                            for (var i = 0; i < count; i++)
                            {
                                values.Add($"{ippGetInteger(attr, i)}");
                            }
                        }
                        else if (valueTag == (int)IppTag.IPP_TAG_BOOLEAN)
                        {
                            values.Add(ippGetBoolean(attr, 0) != 0 ? "true" : "false");
                        }
                        else
                        {
                            var count = ippGetCount(attr);
                            for (var i = 0; i < count; i++)
                            {
                                var valuePtr = ippGetString(attr, i, IntPtr.Zero);
                                if (valuePtr != IntPtr.Zero)
                                {
                                    var value = Marshal.PtrToStringAnsi(valuePtr);
                                    if (value != null)
                                    {
                                        values.Add(value);
                                    }
                                }
                            }
                        }

                        if (values.Count > 0)
                        {
                            attributes[attrName] = values;
                        }
                    }
                }

                attr = ippNextAttribute(response);
            }

            return attributes;
        }
        finally
        {
            if (request != IntPtr.Zero)
            {
                ippDelete(request);
            }
            if (response != IntPtr.Zero)
            {
                ippDelete(response);
            }
            if (http != IntPtr.Zero)
            {
                httpClose(http);
            }
        }
    }

    public static PrinterDetail GetPrinterDetail(string printer)
    {
        var http = IntPtr.Zero;
        var request = IntPtr.Zero;
        var response = IntPtr.Zero;
        try
        {
            http = httpConnectEncrypt("localhost", 631, 0);
            if (http == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to connect cups server. error=[{cupsLastError()}], message=[{GetLastErrorMessage()}]");
            }

            // Create IPP request
            request = ippNewRequest(IPP_OP_GET_PRINTER_ATTRIBUTES);
            if (request == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create ipp request. error=[{cupsLastError()}], message=[{GetLastErrorMessage()}]");
            }

            // Add request attributes
            var printerUri = $"ipp://localhost/printers/{printer}";
            ippAddString(request, IPP_TAG_OPERATION, IPP_TAG_URI, "printer-uri", null, printerUri);
            ippAddString(request, IPP_TAG_OPERATION, IPP_TAG_KEYWORD, "requested-attributes", null, "all");

            // Do request
            response = cupsDoRequest(http, request, $"/printers/{printer}");
            request = IntPtr.Zero;

            if (response == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to get ipp response. error=[{cupsLastError()}], message=[{GetLastErrorMessage()}]");
            }

            return new PrinterDetail(
                printer,
                GetAttributeString(response, "printer-info") ?? string.Empty,
                GetAttributeString(response, "printer-location") ?? string.Empty,
                GetAttributeString(response, "printer-make-and-model") ?? string.Empty,
                (PrinterState)GetAttributeInteger(response, "printer-state"),
                GetAttributeString(response, "printer-state-message") ?? string.Empty,
                GetAttributeBoolean(response, "printer-is-accepting-jobs"),
                GetAttributeStringArray(response, "media-supported"),
                GetAttributeStringArray(response, "media-type-supported"),
                GetAttributeResolution(response),
                GetAttributeStringArray(response, "print-color-mode-supported").Contains("color"),
                GetAttributeStringArray(response, "sides-supported").Count > 1);
        }
        finally
        {
            if (request != IntPtr.Zero)
            {
                ippDelete(request);
            }
            if (response != IntPtr.Zero)
            {
                ippDelete(response);
            }
            if (http != IntPtr.Zero)
            {
                httpClose(http);
            }
        }
    }

    private static string? GetAttributeString(IntPtr ipp, string name)
    {
        var attr = ippFindAttribute(ipp, name, (int)IppTag.IPP_TAG_ZERO);
        if (attr == IntPtr.Zero)
        {
            return null;
        }

        var valuePtr = ippGetString(attr, 0, IntPtr.Zero);
        return valuePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(valuePtr) : null;
    }

    private static int GetAttributeInteger(IntPtr ipp, string name)
    {
        var attr = ippFindAttribute(ipp, name, (int)IppTag.IPP_TAG_ZERO);
        if (attr == IntPtr.Zero)
        {
            return 0;
        }

        return ippGetInteger(attr, 0);
    }

    private static bool GetAttributeBoolean(IntPtr ipp, string name)
    {
        var attr = ippFindAttribute(ipp, name, (int)IppTag.IPP_TAG_ZERO);
        if (attr == IntPtr.Zero)
        {
            return false;
        }

        return ippGetBoolean(attr, 0) != 0;
    }

    private static List<string> GetAttributeStringArray(IntPtr ipp, string name)
    {
        var result = new List<string>();
        var attr = ippFindAttribute(ipp, name, (int)IppTag.IPP_TAG_ZERO);
        if (attr == IntPtr.Zero)
        {
            return result;
        }

        // Max 100
        for (var i = 0; i < 100; i++)
        {
            var valuePtr = ippGetString(attr, i, IntPtr.Zero);
            if (valuePtr == IntPtr.Zero)
            {
                break;
            }

            var value = Marshal.PtrToStringAnsi(valuePtr);
            if (value != null)
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static List<PrinterResolution> GetAttributeResolution(IntPtr ipp)
    {
        var result = new List<PrinterResolution>();

        var attr = ippFindAttribute(ipp, "printer-resolution-supported", (int)IppTag.IPP_TAG_RESOLUTION);
        if (attr == IntPtr.Zero)
        {
            attr = ippFindAttribute(ipp, "printer-resolution-default", (int)IppTag.IPP_TAG_RESOLUTION);
            if (attr == IntPtr.Zero)
            {
                return result;
            }
        }

        var count = ippGetCount(attr);
        for (var i = 0; i < count; i++)
        {
            var status = ippGetResolution(attr, i, out var x, out var y, out var units);
            if (status != 0)
            {
                var resolution = new PrinterResolution
                {
                    XResolution = x,
                    YResolution = y,
                    Units = units == IPP_RES_PER_INCH ? "dpi" : "dpcm"
                };
                result.Add(resolution);
            }
        }

        return result;
    }

    //--------------------------------------------------------------------------------
    // Print
    //--------------------------------------------------------------------------------

    public static int PrintFile(string path, string? printer = null, string jobTitle = "Print Job")
    {
        printer ??= GetDefaultPrinter();
        if (String.IsNullOrEmpty(printer))
        {
            throw new InvalidOperationException("No printer specified and no default printer found.");
        }

        var jobId = cupsPrintFile(printer, path, jobTitle, 0, IntPtr.Zero);
        if (jobId == 0)
        {
            throw new InvalidOperationException($"Failed to print file. error=[{cupsLastError()}], message=[{GetLastErrorMessage()}]");
        }

        return jobId;
    }

    //public static int PrintStream(Stream stream, PrintOptions? options)
    //{
    //    // TODO direct
    //    return 0;
    //}

    //--------------------------------------------------------------------------------
    // Job
    //--------------------------------------------------------------------------------

    public static unsafe IReadOnlyList<PrintJob> GetJobs(string? printer = null, bool myJobsOnly = false)
    {
        var jobs = new List<PrintJob>();
        var jobsPtr = IntPtr.Zero;
        try
        {
            var numJobs = cupsGetJobs(ref jobsPtr, printer ?? string.Empty, myJobsOnly ? 1 : 0, -1);
            for (var i = 0; i < numJobs; i++)
            {
                var currentJobPtr = IntPtr.Add(jobsPtr, i * sizeof(cups_job_t));
                var job = (cups_job_t*)currentJobPtr;

                jobs.Add(new PrintJob(
                    job->id,
                    Marshal.PtrToStringAnsi(job->title) ?? string.Empty,
                    Marshal.PtrToStringAnsi(job->dest) ?? string.Empty,
                    Marshal.PtrToStringAnsi(job->user) ?? string.Empty,
                    DateTimeOffset.FromUnixTimeSeconds(job->creation_time).LocalDateTime,
                    (PrintJobState)job->state));
            }

            return jobs;
        }
        finally
        {
            if (jobsPtr != IntPtr.Zero)
            {
                cupsFreeJobs(jobs.Count, jobsPtr);
            }
        }
    }

    public static bool CancelJob(string printer, int jobId)
    {
        var result = cupsCancelJob(printer, jobId);
        return result == 1;
    }
}
