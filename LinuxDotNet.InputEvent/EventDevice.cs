namespace LinuxDotNet.InputEvent;

using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

using static LinuxDotNet.InputEvent.NativeMethods;

[SupportedOSPlatform("linux")]
public sealed unsafe class EventDevice : IDisposable
{
    private static readonly int BufferSize = sizeof(input_event);

    private readonly string path;

    private byte[] buffer;

    private bool grabbed;

    private FileStream? stream;

    public bool IsOpen => stream is not null;

    public EventDevice(string path)
    {
        this.path = path;
        buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    }

    public void Dispose()
    {
        Close();
        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
        }
    }

    public string? GetDeviceName()
    {
        if (stream is null)
        {
            return null;
        }

        var fd = (int)stream.SafeFileHandle.DangerousGetHandle();
        Span<byte> buff = stackalloc byte[256];
        fixed (byte* ptr = buff)
        {
            var result = ioctl(fd, EVIOCGNAME, ptr);
            if (result >= 0)
            {
                return Encoding.UTF8.GetString(buffer, 0, result);
            }
        }

        return null;
    }

    public bool Open(bool grab = false)
    {
        if (stream is not null)
        {
            return false;
        }

        stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0, false);

        if (grab)
        {
            var fd = (int)stream.SafeFileHandle.DangerousGetHandle();
            var result = ioctl(fd, EVIOCGRAB, 1);
            if (result < 0)
            {
                stream.Dispose();
                stream = null;
                throw new IOException($"Grab failed. error=[{Marshal.GetLastWin32Error()}]");
            }
        }

        return true;
    }

    public void Close()
    {
        if (stream is null)
        {
            return;
        }

        if (grabbed)
        {
            var fd = (int)stream.SafeFileHandle.DangerousGetHandle();
            _ = ioctl(fd, EVIOCGRAB, 0);
            grabbed = false;
        }

        stream.Dispose();
        stream = null;
    }

    public bool Read(out EventResult result, int timeout = -1)
    {
        if (stream is null)
        {
            result = default;
            return false;
        }

        var fd = (int)stream.SafeFileHandle.DangerousGetHandle();

        var pollFd = new pollFd
        {
            fd = fd,
            events = POLLIN,
            revents = 0
        };

        var pollResult = poll(ref pollFd, 1, timeout);
        if (pollResult == 0)
        {
            // Timeout
            result = default;
            return false;
        }

        if (pollResult < 0)
        {
            var error = Marshal.GetLastWin32Error();
            if (error == EINTR)
            {
                // Interrupted
                result = default;
                return false;
            }

            throw new IOException($"Pool failed. error=[{Marshal.GetLastWin32Error()}]");
        }

        if ((pollFd.revents & POLLERR) != 0)
        {
            throw new IOException("Pool device error.");
        }
        if ((pollFd.revents & POLLHUP) != 0)
        {
            throw new IOException("Pool device disconnected.");
        }
        if ((pollFd.revents & POLLNVAL) != 0)
        {
            throw new IOException("Pool invalid file descriptor.");
        }

        if ((pollFd.revents & POLLIN) == 0)
        {
            // No data
            result = default;
            return false;
        }

        var bytesRead = stream.Read(buffer, 0, BufferSize);
        if (bytesRead < BufferSize)
        {
            throw new IOException("Device disconnected or stream ended unexpectedly.");
        }

        fixed (byte* ptr = buffer)
        {
            var ie = (input_event*)ptr;
            result = new EventResult
            {
                Timestamp = new TimeSpan((ie->tv_sec * 10000000) + (ie->tv_usec * 10)),
                Type = (EventType)ie->type,
                Code = ie->code,
                Value = ie->value
            };
        }

        return true;
    }
}
