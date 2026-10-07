namespace LinuxDotNet.SystemInfo;

using System;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class TcpStat : IDisposable
{
    private readonly KernelFile file;

    private bool disposed;

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

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private TcpStat(KernelFile file)
    {
        this.file = file;
    }

    internal static TcpStat Create(int? version)
    {
        var instance = new TcpStat(new KernelFile($"/proc/net/tcp{version}"));
        instance.Update();
        return instance;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        file.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

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

        if (!file.Read())
        {
            return false;
        }

        var remaining = file.Content;
        _ = TryReadLine(ref remaining, out _);
        while (TryReadLine(ref remaining, out var line))
        {
            // sl local_address rem_address st ... (5 or more tokens)
            _ = NextToken(ref line);
            _ = NextToken(ref line);
            _ = NextToken(ref line);
            var state = NextToken(ref line);
            if (NextToken(ref line).IsEmpty)
            {
                continue;
            }

            switch (ParseHex(state))
            {
                case 0x01:
                    Established++;
                    break;
                case 0x02:
                    SynSent++;
                    break;
                case 0x03:
                    SynRecv++;
                    break;
                case 0x04:
                    FinWait1++;
                    break;
                case 0x05:
                    FinWait2++;
                    break;
                case 0x06:
                    TimeWait++;
                    break;
                case 0x07:
                    Close++;
                    break;
                case 0x08:
                    CloseWait++;
                    break;
                case 0x09:
                    LastAck++;
                    break;
                case 0x0A:
                    Listen++;
                    break;
                case 0x0B:
                    Closing++;
                    break;
            }

            Total++;
        }

        UpdateAt = DateTime.Now;

        return true;
    }
}
