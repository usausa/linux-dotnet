namespace LinuxDotNet.Cups;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using static LinuxDotNet.Cups.NativeMethods;

[SupportedOSPlatform("linux")]
public static class CupsPrinter
{
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
                var printerName = Marshal.PtrToStringAnsi(dest->name);
                if (!String.IsNullOrEmpty(printerName))
                {
                    var info = new PrinterInfo(
                        printerName,
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

    public static int PrintFile(string path, string? printer = null, string jobTitle = "Print Job")
    {
        var printerName = printer ?? GetDefaultPrinter();
        if (String.IsNullOrEmpty(printerName))
        {
            throw new InvalidOperationException("No printer specified and no default printer found.");
        }

        var jobId = cupsPrintFile(printerName, path, jobTitle, 0, IntPtr.Zero);
        if (jobId == 0)
        {
            var errorPtr = cupsLastErrorString();
            var errorMessage = Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown error";
            throw new InvalidOperationException($"Failed to print file. CUPS Error: {errorMessage}");
        }

        return jobId;
    }
}
