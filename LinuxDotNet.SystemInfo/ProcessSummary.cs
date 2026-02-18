namespace LinuxDotNet.SystemInfo;

public sealed class ProcessSummary
{
    public DateTime UpdateAt { get; private set; }

    public int ProcessCount { get; private set; }

    public int ThreadCount { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal ProcessSummary()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        var process = 0;
        var thread = 0;
        foreach (var dir in Directory.EnumerateDirectories("/proc"))
        {
            if (!Int32.TryParse(Path.GetFileName(dir), out _))
            {
                continue;
            }

            process++;

            var statusFilePath = Path.Combine(dir, "status");
            if (!File.Exists(statusFilePath))
            {
                continue;
            }

#pragma warning disable CA1031
            try
            {
                using var reader = new StreamReader(statusFilePath);
                while (reader.ReadLine() is { } line)
                {
                    var span = line.AsSpan();

                    if (span.StartsWith("Threads:"))
                    {
                        thread += ExtractInt32(span);
                    }
                }
            }
            catch
            {
                // Ignore
            }
        }
#pragma warning restore CA1031

        ProcessCount = process;
        ThreadCount = thread;

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static int ExtractInt32(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, '\t', StringSplitOptions.RemoveEmptyEntries) > 1) && Int32.TryParse(span[range[1]], out var result) ? result : 0;
    }
}
