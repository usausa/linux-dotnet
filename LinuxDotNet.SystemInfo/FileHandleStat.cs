namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

public sealed class FileHandleStat
{
    public DateTime UpdateAt { get; private set; }

    public ulong Allocated { get; private set; }

    public ulong Used { get; private set; }

    public ulong Max { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal FileHandleStat()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        if (!FileHelper.TryReadText("/proc/sys/fs/file-nr", out var text))
        {
            return false;
        }

        var span = text.AsSpan();
        var range = (Span<Range>)stackalloc Range[4];
        span.Split(range, '\t', StringSplitOptions.RemoveEmptyEntries);
        Allocated = ParseUInt64(span[range[0]]);
        Used = ParseUInt64(span[range[1]]);
        Max = ParseUInt64(span[range[2]]);

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static ulong ParseUInt64(ReadOnlySpan<char> source)
    {
        return UInt64.TryParse(source, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
