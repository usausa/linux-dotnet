namespace LinuxDotNet.Cups;

public enum PrintJobState
{
    Unknown = 0,
    Pending = 3,
    Held = 4,
    Processing = 5,
    Stopped = 6,
    Canceled = 7,
    Aborted = 8,
    Completed = 9
}

public sealed class PrintJob
{
    public int JobId { get; }

    public string Title { get; }

    public string Printer { get; }

    public string User { get; }

    public DateTime SubmitTime { get; }

    public PrintJobState State { get; }

    public PrintJob(int jobId, string title, string printer, string user, DateTime submitTime, PrintJobState state)
    {
        JobId = jobId;
        Title = title;
        Printer = printer;
        User = user;
        SubmitTime = submitTime;
        State = state;
    }
}
