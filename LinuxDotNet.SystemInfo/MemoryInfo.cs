namespace LinuxDotNet.SystemInfo;

using System;

public sealed class MemoryInfo
{
    public DateTime UpdateAt { get; private set; }

    public long Total { get; private set; }

    public long Free { get; private set; }

    public long Buffers { get; private set; }

    public long Cached { get; private set; }

    public double Usage => Total == 0 ? 0 : (double)(Total - Free - Buffers - Cached) / Total * 100;

    internal MemoryInfo()
    {
        Update();
    }

    public bool Update()
    {
        using var reader = new StreamReader("/proc/meminfo");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            if (span.StartsWith("MemTotal"))
            {
                Total = ExtractInt64(span);
            }
            else if (span.StartsWith("MemFree"))
            {
                Free = ExtractInt64(span);
            }
            else if (span.StartsWith("Buffers"))
            {
                Buffers = ExtractInt64(span);
            }
            else if (span.StartsWith("Cached"))
            {
                Cached = ExtractInt64(span);
            }
        }

        UpdateAt = DateTime.Now;

        return true;
    }

    private static long ExtractInt64(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[2];
        return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && Int64.TryParse(span[range[1]], out var result) ? result : 0;
    }
}
