namespace LinuxDotNet.GameInput;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

// TODO Start/Stop
public sealed class GameController : IDisposable
{
    public event Action<byte, bool>? ButtonChanged;

    public event Action<byte, short>? AxisChanged;

    public event Action<bool>? ConnectionChanged;

    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan ShutdownWait = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource cts = new();

    private readonly Task processingTask;

    private readonly string deviceFile;

    private bool connected;

    private bool[] buttonInitialized;
    private bool[] axisInitialized;
    private bool[] buttons;
    private short[] axis;

    public bool IsConnected => Volatile.Read(ref connected);

    public GameController(string deviceFile = "/dev/input/js0")
    {
        this.deviceFile = deviceFile;

        buttonInitialized = ArrayPool<bool>.Shared.Rent(256);
        axisInitialized = ArrayPool<bool>.Shared.Rent(256);
        buttons = ArrayPool<bool>.Shared.Rent(256);
        axis = ArrayPool<short>.Shared.Rent(256);
        buttonInitialized.AsSpan().Clear();
        axisInitialized.AsSpan().Clear();
        buttons.AsSpan().Clear();
        axis.AsSpan().Clear();

        processingTask = Task.Run(() => ProcessAsync(cts.Token), cts.Token);
    }

    public void Dispose()
    {
        if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
        }

        try
        {
            processingTask.Wait(ShutdownWait);
        }
        catch (AggregateException)
        {
        }

        cts.Dispose();

        if (buttonInitialized.Length > 0)
        {
            ArrayPool<bool>.Shared.Return(buttonInitialized);
            buttonInitialized = [];
        }
        if (axisInitialized.Length > 0)
        {
            ArrayPool<bool>.Shared.Return(axisInitialized);
            axisInitialized = [];
        }
        if (buttons.Length > 0)
        {
            ArrayPool<bool>.Shared.Return(buttons);
            buttons = [];
        }
        if (axis.Length > 0)
        {
            ArrayPool<short>.Shared.Return(axis);
            axis = [];
        }
    }

    public bool GetButtonPressed(byte address) => Volatile.Read(ref buttons[address]);

    public short GetAxis(byte address) => Volatile.Read(ref axis[address]);

    private void UpdateConnectionState(bool value)
    {
        if (connected == value)
        {
            return;
        }

        Volatile.Write(ref connected, value);

        if (value)
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                Volatile.Write(ref buttons[i], false);
            }
            for (var i = 0; i < axis.Length; i++)
            {
                Volatile.Write(ref axis[i], 0);
            }
            buttonInitialized.AsSpan().Clear();
            axisInitialized.AsSpan().Clear();
        }

        ConnectionChanged?.Invoke(value);
    }

    private async Task ProcessAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
#pragma warning disable CA1031
            try
            {
#pragma warning disable CA2007
                await using var stream = await WaitForDeviceAsync(token).ConfigureAwait(false);
#pragma warning restore CA2007
                if (token.IsCancellationRequested || (stream is null))
                {
                    break;
                }

                // Connected
                UpdateConnectionState(true);

                await ProcessMessagesAsync(stream, token).ConfigureAwait(false);
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

    private async ValueTask<FileStream?> WaitForDeviceAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (File.Exists(deviceFile))
            {
#pragma warning disable CA1031
                try
                {
                    return new FileStream(deviceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch
                {
                    // Ignore
                }
#pragma warning restore CA1031
            }

            await Task.Delay(ReconnectInterval, token).ConfigureAwait(false);
        }

        return null;
    }

    private async ValueTask ProcessMessagesAsync(FileStream stream, CancellationToken token)
    {
        const int messageSize = 8;
        var message = ArrayPool<byte>.Shared.Rent(messageSize);

        while (!token.IsCancellationRequested)
        {
            var bytesRead = await stream.ReadAsync(message.AsMemory(0, messageSize), token).ConfigureAwait(false);
            if (bytesRead < messageSize)
            {
                throw new IOException("Device disconnected or stream ended unexpectedly.");
            }

            var address = GetAddress(message);
            if (HasConfiguration(message))
            {
                if (IsButton(message))
                {
                    buttonInitialized[address] = true;
                    buttons[address] = false;
                }
                if (IsAxis(message))
                {
                    axisInitialized[address] = true;
                    axis[address] = 0;
                }
            }
            else
            {
                if (IsButton(message))
                {
                    var newValue = IsButtonPressed(message);
                    if (!buttonInitialized[address] || (buttons[address] != newValue))
                    {
                        buttonInitialized[address] = true;
                        Volatile.Write(ref buttons[address], newValue);

                        ButtonChanged?.Invoke(address, newValue);
                    }
                }
                if (IsAxis(message))
                {
                    var newValue = GetAxisValue(message);
                    if (!axisInitialized[address] || (axis[address] != newValue))
                    {
                        axisInitialized[address] = true;
                        Volatile.Write(ref axis[address], newValue);

                        AxisChanged?.Invoke(address, newValue);
                    }
                }
            }
        }

        ArrayPool<byte>.Shared.Return(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasConfiguration(ReadOnlySpan<byte> message) => IsFlagSet(message[6], 0x80);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsButton(ReadOnlySpan<byte> message) => IsFlagSet(message[6], 0x01);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAxis(ReadOnlySpan<byte> message) => IsFlagSet(message[6], 0x02);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsButtonPressed(ReadOnlySpan<byte> message) => message[4] == 0x01;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetAddress(ReadOnlySpan<byte> message) => message[7];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static short GetAxisValue(ReadOnlySpan<byte> message) => BinaryPrimitives.ReadInt16LittleEndian(message[4..6]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsFlagSet(byte value, byte flag) => (value & flag) == flag;
}
