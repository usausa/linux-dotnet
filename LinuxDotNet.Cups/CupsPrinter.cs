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
}
