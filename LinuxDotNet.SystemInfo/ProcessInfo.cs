namespace LinuxDotNet.SystemInfo;

using static LinuxDotNet.SystemInfo.NativeMethods;

public enum ProcessState
{
    Unknown,
    Running,
    Sleeping,
    DiskSleep,
    Zombie,
    Stopped,
    TracingStop,
    Dead,
    WakeKill,
    Waking,
    Parked,
    Idle
}

public sealed record ProcessInfo
{
    private static readonly ulong ClockTick = (ulong)GetClockTick();
    private static readonly long BootTime = GetBootTime();
    private static readonly ulong PageSize = (ulong)Environment.SystemPageSize;

    // Basic

    public int ProcessId { get; private init; }

    public int ParentProcessId { get; private set; }

    public string Name { get; init; } = default!;

    // Scheduler

    public int Priority { get; private set; }

    public int Nice { get; private set; }

    // Thread

    public int ThreadCount { get; private set; }

    // CPU

    public ProcessState State { get; private set; }

    public ulong UserTime { get; private set; }

    public ulong SystemTime { get; private set; }

    public DateTimeOffset StartTime { get; private set; }

    // Memory

    public ulong VirtualSize { get; private set; }

    public ulong ResidentSize { get; private set; }

    // I/O

    public long MajorFaults { get; private set; }

    public long MinorFaults { get; private set; }

    // Identify

    public uint UserId { get; private set; }

    public uint GroupId { get; private set; }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static long GetClockTick()
    {
#pragma warning disable CA1031
        try
        {
            return sysconf(SC_CLK_TCK);
        }
        catch
        {
            return 100; // Default value
        }
    }
#pragma warning restore CA1031

    // ReSharper disable StringLiteralTypo
    private static long GetBootTime()
    {
        using var reader = new StreamReader("/proc/stat");
        while (reader.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            if (span.StartsWith("btime"))
            {
                return ExtractInt64(span);
            }
        }

        return 0;

        static long ExtractInt64(ReadOnlySpan<char> span)
        {
            var range = (Span<Range>)stackalloc Range[3];
            return (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) > 1) && Int64.TryParse(span[range[1]], out var result) ? result : 0;
        }
    }
    // ReSharper restore StringLiteralTypo

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<ProcessInfo> GetProcesses()
    {
        var result = new List<ProcessInfo>();

        foreach (var dir in Directory.EnumerateDirectories("/proc"))
        {
            var name = Path.GetFileName(dir).AsSpan();
            if (!Int32.TryParse(name, out var pid))
            {
                continue;
            }

            var entry = GetProcess(pid);
            if (entry is not null)
            {
                result.Add(entry);
            }
        }

        result.Sort(static (x, y) => x.ProcessId.CompareTo(y.ProcessId));
        return result;
    }

    public static ProcessInfo? GetProcess(int processId)
    {
        var procPath = $"/proc/{processId}";
        if (!Directory.Exists(procPath))
        {
            return null;
        }

#pragma warning disable CA1031
        try
        {
            var statPath = Path.Combine(procPath, "stat");
            if (!File.Exists(statPath))
            {
                return null;
            }

            var statContent = File.ReadAllText(statPath).AsSpan();
            var commStart = statContent.IndexOf('(');
            var commEnd = statContent.LastIndexOf(')');
            if ((commStart < 0) || (commEnd < 0) || (commEnd <= commStart))
            {
                return null;
            }

            var name = statContent.Slice(commStart + 1, commEnd - commStart - 1);
            var rest = statContent[(commEnd + 2)..];

            var statRange = (Span<Range>)stackalloc Range[23];
            var statCount = rest.Split(statRange, ' ', StringSplitOptions.RemoveEmptyEntries);
            if (statCount < 22)
            {
                return null;
            }

            var result = new ProcessInfo
            {
                ProcessId = processId,
                Name = name.ToString()
            };

            var stateChar = rest[statRange[0]].Length > 0 ? rest[statRange[0]][0] : '?';
            result.State = stateChar switch
            {
                'R' => ProcessState.Running,
                'S' => ProcessState.Sleeping,
                'D' => ProcessState.DiskSleep,
                'Z' => ProcessState.Zombie,
                'T' => ProcessState.Stopped,
                't' => ProcessState.TracingStop,
                'X' or 'x' => ProcessState.Dead,
                'K' => ProcessState.WakeKill,
                'W' => ProcessState.Waking,
                'P' => ProcessState.Parked,
                'I' => ProcessState.Idle,
                _ => ProcessState.Unknown
            };

            result.ParentProcessId = Int32.TryParse(rest[statRange[1]], out var parentProcessId) ? parentProcessId : 0;
            result.MinorFaults = Int64.TryParse(rest[statRange[7]], out var minorFault) ? minorFault : 0;
            result.MajorFaults = Int64.TryParse(rest[statRange[9]], out var majorFault) ? majorFault : 0;
            result.UserTime = UInt64.TryParse(rest[statRange[11]], out var userTime) ? userTime : 0;
            result.SystemTime = UInt64.TryParse(rest[statRange[12]], out var systemTime) ? systemTime : 0;
            result.Priority = Int32.TryParse(rest[statRange[15]], out var priority) ? priority : 0;
            result.Nice = Int32.TryParse(rest[statRange[16]], out var nice) ? nice : 0;
            result.ThreadCount = Int32.TryParse(rest[statRange[17]], out var threadCount) ? threadCount : 0;
            result.StartTime = UInt64.TryParse(rest[statRange[19]], out var startTimeTicks)
                ? DateTimeOffset.FromUnixTimeSeconds(BootTime + (long)(startTimeTicks / ClockTick))
                : DateTimeOffset.MinValue;
            result.VirtualSize = UInt64.TryParse(rest[statRange[20]], out var virtualSize) ? virtualSize : 0;
            result.ResidentSize = Int64.TryParse(rest[statRange[21]], out var rss) ? (ulong)rss * PageSize : 0;

            // Status
            var statusPath = Path.Combine(procPath, "status");
            if (File.Exists(statusPath))
            {
                using var reader = new StreamReader(statusPath);
                while (reader.ReadLine() is { } line)
                {
                    var span = line.AsSpan();

                    if (span.StartsWith("Uid:"))
                    {
                        result.UserId = ExtractStatUInt32(span);
                    }
                    else if (span.StartsWith("Gid:"))
                    {
                        result.GroupId = ExtractStatUInt32(span);
                    }
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static uint ExtractStatUInt32(ReadOnlySpan<char> span)
    {
        var range = (Span<Range>)stackalloc Range[3];
        return (span.Split(range, '\t', StringSplitOptions.RemoveEmptyEntries) > 1) && UInt32.TryParse(span[range[1]], out var result) ? result : 0;
    }
}
