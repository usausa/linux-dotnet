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

    public static IReadOnlyList<PrinterInfo> GetPrinters()
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

            // TODO unsafe & options
            var size = Marshal.SizeOf<cups_dest_t>();
            for (var i = 0; i < num; i++)
            {
                var currentPtr = IntPtr.Add(ptr, i * size);
                var dest = Marshal.PtrToStructure<cups_dest_t>(currentPtr);
                var printerName = Marshal.PtrToStringAnsi(dest.name);
                if (!String.IsNullOrEmpty(printerName))
                {
                    printers.Add(new PrinterInfo(printerName, dest.isDefault != 0));
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
