namespace Example.InputEvent.ConsoleApp;

using System.IO;
using System.Text;

using LinuxDotNet.InputEvent;

internal sealed class BarcodeReader : IDisposable
{
    private const int DevicePollTimeout = 100;

    public event Action<string>? BarcodeScanned;

    public event Action<bool>? ConnectionChanged;

    private static readonly Dictionary<ushort, char> KeyMap = new()
    {
        { 2, '1' }, { 3, '2' }, { 4, '3' }, { 5, '4' }, { 6, '5' },
        { 7, '6' }, { 8, '7' }, { 9, '8' }, { 10, '9' }, { 11, '0' },
        { 16, 'Q' }, { 17, 'W' }, { 18, 'E' }, { 19, 'R' }, { 20, 'T' },
        { 21, 'Y' }, { 22, 'U' }, { 23, 'I' }, { 24, 'O' }, { 25, 'P' },
        { 30, 'A' }, { 31, 'S' }, { 32, 'D' }, { 33, 'F' }, { 34, 'G' },
        { 35, 'H' }, { 36, 'J' }, { 37, 'K' }, { 38, 'L' },
        { 44, 'Z' }, { 45, 'X' }, { 46, 'C' }, { 47, 'V' }, { 48, 'B' },
        { 49, 'N' }, { 50, 'M' },
        { 57, ' ' },
        { 12, '-' },
        { 13, '=' },
        { 26, '[' }, { 27, ']' }, { 39, ';' }, { 40, '\'' },
        { 41, '`' }, { 43, '\\' }, { 51, ',' }, { 52, '.' }, { 53, '/' }
    };

    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan StopWait = TimeSpan.FromSeconds(5);

    private readonly string deviceFile;

    private readonly EventDevice device;

    private readonly StringBuilder buffer = new();

    private CancellationTokenSource? cts;

    private Task? processingTask;

    private bool connected;

    public bool IsConnected => Volatile.Read(ref connected);

    public BarcodeReader(string deviceFile)
    {
        this.deviceFile = deviceFile;
        device = new EventDevice(deviceFile);
    }

    public void Dispose()
    {
        Stop();
        device.Dispose();
    }

    public void Start()
    {
        if (processingTask is not null)
        {
            return;
        }

        cts = new CancellationTokenSource();
        processingTask = Task.Run(() => ProcessAsync(cts.Token), cts.Token);
    }

    public void Stop()
    {
        if ((cts is null) || (processingTask is null))
        {
            return;
        }

        if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
        }

        try
        {
            processingTask.Wait(StopWait);
        }
        catch (AggregateException)
        {
        }

        cts.Dispose();
        cts = null;
        processingTask = null;
    }

    private void UpdateConnectionState(bool value)
    {
        if (connected == value)
        {
            return;
        }

        Volatile.Write(ref connected, value);

        ConnectionChanged?.Invoke(value);
    }

    private async Task ProcessAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
#pragma warning disable CA1031
            try
            {
                var result = await WaitForDeviceAsync(token).ConfigureAwait(false);
                if (token.IsCancellationRequested || !result)
                {
                    break;
                }

                // Connected
                UpdateConnectionState(true);

                ProcessMessages(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Disconnected
                UpdateConnectionState(false);

                // Retry wait
                try
                {
                    await Task.Delay(ReconnectInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
#pragma warning restore CA1031
        }

        // Disconnected
        UpdateConnectionState(false);
    }

    private void ProcessMessages(CancellationToken token)
    {
        buffer.Clear();

        while (!token.IsCancellationRequested)
        {
            if (device.Read(out var result, DevicePollTimeout) &&
                (result.Type == EventType.Key) &&
                ((EventValue)result.Value == EventValue.Pressed))
            {
                if (result.Code == 28) // Enter
                {
                    BarcodeScanned?.Invoke(buffer.ToString());
                    buffer.Clear();
                }
                else if (KeyMap.TryGetValue(result.Code, out var c))
                {
                    buffer.Append(c);
                }
            }
        }
    }

    private async ValueTask<bool> WaitForDeviceAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (File.Exists(deviceFile))
            {
#pragma warning disable CA1031
                try
                {
                    device.Open(true);
                    return true;
                }
                catch
                {
                    // Ignore
                }
#pragma warning restore CA1031
            }

            await Task.Delay(ReconnectInterval, token).ConfigureAwait(false);
        }

        return false;
    }
}
