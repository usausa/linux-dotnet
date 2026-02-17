namespace LinuxDotNet.SystemInfo;

using System;
using System.Globalization;

public sealed class FileHandleStat
{
    public DateTime UpdateAt { get; private set; }

    public long Allocated { get; private set; }

    public long Used { get; private set; }

    public long Max { get; private set; }

    internal FileHandleStat()
    {
        Update();
    }

    public bool Update()
    {
        var span = File.ReadAllText("/proc/sys/fs/file-nr").AsSpan();
        var range = (Span<Range>)stackalloc Range[4];
        span.Split(range, '\t', StringSplitOptions.RemoveEmptyEntries);
        Allocated = ParseInt64(span[range[0]]);
        Used = ParseInt64(span[range[1]]);
        Max = ParseInt64(span[range[2]]);

        UpdateAt = DateTime.Now;

        return true;
    }

    private static long ParseInt64(ReadOnlySpan<char> source)
    {
        return Int64.TryParse(source, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
