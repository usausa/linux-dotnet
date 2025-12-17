namespace LinuxDotNet.SystemInfo;

using System;

public sealed class TcpInfo
{
    private readonly string path;

    public DateTime UpdateAt { get; private set; }

    // ReSharper disable IdentifierTypo
    public int Established { get; private set; }

    public int SynSent { get; private set; }

    public int SynRecv { get; private set; }

    public int FinWait1 { get; private set; }

    public int FinWait2 { get; private set; }

    public int TimeWait { get; private set; }

    public int Close { get; private set; }

    public int CloseWait { get; private set; }

    public int LastAck { get; private set; }

    public int Listen { get; private set; }

    public int Closing { get; private set; }

    public int Total { get; private set; }
    // ReSharper restore IdentifierTypo

    internal TcpInfo(int? version = null)
    {
        path = $"/proc/net/tcp{version}";
        Update();
    }

    public bool Update()
    {
        Established = 0;
        SynSent = 0;
        SynRecv = 0;
        FinWait1 = 0;
        FinWait2 = 0;
        TimeWait = 0;
        Close = 0;
        CloseWait = 0;
        LastAck = 0;
        Listen = 0;
        Closing = 0;
        Total = 0;

        var range = (Span<Range>)stackalloc Range[5];
        using var reader = new StreamReader(path);
        reader.ReadLine();
        while (reader.ReadLine() is { } line)
        {
            range.Clear();
            var span = line.AsSpan();
            if (span.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries) < 5)
            {
                continue;
            }

            var stat = span[range[3]];
            switch (stat)
            {
                case "01":
                    Established++;
                    break;
                case "02":
                    SynSent++;
                    break;
                case "03":
                    SynRecv++;
                    break;
                case "04":
                    FinWait1++;
                    break;
                case "05":
                    FinWait2++;
                    break;
                case "06":
                    TimeWait++;
                    break;
                case "07":
                    Close++;
                    break;
                case "08":
                    CloseWait++;
                    break;
                case "09":
                    LastAck++;
                    break;
                case "0A":
                    Listen++;
                    break;
                case "0B":
                    Closing++;
                    break;
            }

            Total++;
        }

        UpdateAt = DateTime.Now;

        return true;
    }
}
