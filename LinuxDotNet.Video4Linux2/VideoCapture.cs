namespace LinuxDotNet.Video4Linux2;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using static LinuxDotNet.Video4Linux2.NativeMethods;

// ReSharper disable StructCanBeMadeReadOnly
#pragma warning disable CA1815
public readonly struct FrameBuffer
{
    private readonly IntPtr buffer;

    private readonly int length;

#pragma warning disable IDE0032
    public int Length => length;
#pragma warning restore IDE0032

    public bool IsEmpty => buffer == IntPtr.Zero || length == 0;

    public ReadOnlySpan<byte> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (IsEmpty)
            {
                return [];
            }

            unsafe
            {
                return new Span<byte>((void*)buffer, length);
            }
        }
    }

    internal FrameBuffer(IntPtr buffer, int length)
    {
        this.buffer = buffer;
        this.length = length;
    }

    public byte[] ToArray()
    {
        if (IsEmpty)
        {
            return [];
        }

        var array = new byte[length];
        Span.CopyTo(array);
        return array;
    }
}
#pragma warning restore CA1815
// ReSharper restore StructCanBeMadeReadOnly

#pragma warning disable CA1806
public sealed class VideoCapture : IDisposable
{
    private const int StopTimeout = 5000;

    public event Action<FrameBuffer>? FrameCaptured;

#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    private readonly string path;

    private int fd = -1;

    private IntPtr[] buffers = [];

    private int[] bufferLengths = [];

    private Thread? captureThread;

    private CancellationTokenSource? captureCts;

    public int Width { get; private set; }

    public int Height { get; private set; }

    // ReSharper disable once InconsistentlySynchronizedField
    public bool IsOpen => fd >= 0;

    public bool IsCapturing => captureThread is not null;

    public VideoCapture(string path)
    {
        this.path = path;
    }

    public void Dispose()
    {
        Close();
    }

    public bool Open(int width = 640, int height = 480)
    {
        lock (sync)
        {
            return OpenCore(width, height);
        }
    }

    private unsafe bool OpenCore(int width, int height)
    {
        if (IsOpen)
        {
            return false;
        }

        fd = open(path, O_RDWR);
        if (fd < 0)
        {
            return false;
        }

        // Set format
        v4l2_format format = default;
        format.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        format.fmt.pix.width = (uint)width;
        format.fmt.pix.height = (uint)height;
        format.fmt.pix.pixelformat = V4L2_PIX_FMT_YUYV;
        format.fmt.pix.field = V4L2_FIELD_NONE;
        format.fmt.pix.bytesperline = 0;
        format.fmt.pix.sizeimage = 0;
        if (ioctl(fd, VIDIOC_S_FMT, (IntPtr)(&format)) < 0)
        {
            CloseInternal();
            return false;
        }

        Width = (int)format.fmt.pix.width;
        Height = (int)format.fmt.pix.height;

        // Request buffers
        v4l2_requestbuffers requestBuffers;
        requestBuffers.count = 4;
        requestBuffers.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        requestBuffers.memory = V4L2_MEMORY_MMAP;
        requestBuffers.reserved[0] = 0;
        requestBuffers.reserved[1] = 0;
        if (ioctl(fd, VIDIOC_REQBUFS, (IntPtr)(&requestBuffers)) < 0)
        {
            CloseInternal();
            return false;
        }

        buffers = new IntPtr[requestBuffers.count];
        bufferLengths = new int[requestBuffers.count];

        // Map buffers
        for (uint i = 0; i < requestBuffers.count; i++)
        {
            // Query buffer
            var buffer = default(v4l2_buffer);
            buffer.index = i;
            buffer.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            buffer.memory = V4L2_MEMORY_MMAP;
            if (ioctl(fd, VIDIOC_QUERYBUF, (IntPtr)(&buffer)) < 0)
            {
                CloseInternal();
                return false;
            }

            // Memory map
            buffers[i] = mmap(IntPtr.Zero, (int)buffer.length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, (int)buffer.m.offset);
            bufferLengths[i] = (int)buffer.length;
            if (buffers[i] == new IntPtr(-1))
            {
                CloseInternal();
                return false;
            }
        }

        return true;
    }

    private unsafe bool QueueAllBuffers()
    {
        for (var i = 0; i < buffers.Length; i++)
        {
            var buffer = default(v4l2_buffer);
            buffer.index = (uint)i;
            buffer.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            buffer.memory = V4L2_MEMORY_MMAP;
            if (ioctl(fd, VIDIOC_QBUF, (IntPtr)(&buffer)) < 0)
            {
                return false;
            }
        }

        return true;
    }

    public bool SetFrameRate(int fps)
    {
        lock (sync)
        {
            return SetFrameRateCore(fps);
        }
    }

    private unsafe bool SetFrameRateCore(int fps)
    {
        if (!IsOpen || (fps <= 0))
        {
            return false;
        }

        // Get parameters
        v4l2_streamparm parm = default;
        parm.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        if (ioctl(fd, VIDIOC_G_PARM, (IntPtr)(&parm)) < 0)
        {
            return false;
        }

        // Set parameters
        parm.parm.capture.timeperframe.numerator = 1;
        parm.parm.capture.timeperframe.denominator = (uint)fps;
        if (ioctl(fd, VIDIOC_S_PARM, (IntPtr)(&parm)) < 0)
        {
            return false;
        }

        return true;
    }

    public bool Close()
    {
        lock (sync)
        {
            if (!IsOpen)
            {
                return true;
            }

            if (!StopCaptureCore())
            {
                return false;
            }

            CloseInternal();

            return true;
        }
    }

    private void CloseInternal()
    {
        for (var i = 0; i < buffers.Length; i++)
        {
            if ((buffers[i] != IntPtr.Zero) && (buffers[i] != new IntPtr(-1)))
            {
                munmap(buffers[i], bufferLengths[i]);
            }

            buffers[i] = IntPtr.Zero;
        }

        if (fd >= 0)
        {
            close(fd);
        }
        fd = -1;

        Width = 0;
        Height = 0;
    }

    public bool Snapshot(IBufferWriter<byte> writer, int timeout = 5000)
    {
        lock (sync)
        {
            return SnapshotCore(writer, timeout);
        }
    }

    private unsafe bool SnapshotCore(IBufferWriter<byte> writer, int timeout)
    {
        if (!IsOpen || IsCapturing)
        {
            return false;
        }

        if (!QueueAllBuffers())
        {
            return false;
        }

        // Start streaming
        var type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        if (ioctl(fd, VIDIOC_STREAMON, (IntPtr)(&type)) < 0)
        {
            return false;
        }

        try
        {
            if (timeout > 0)
            {
                var fds = new pollfd
                {
                    fd = fd,
                    events = POLLIN
                };
                if (poll(ref fds, 1, timeout) <= 0)
                {
                    return false;
                }
            }

            // De-queue buffer
            var buffer = default(v4l2_buffer);
            buffer.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            buffer.memory = V4L2_MEMORY_MMAP;
            if (ioctl(fd, VIDIOC_DQBUF, (IntPtr)(&buffer)) < 0)
            {
                return false;
            }

            if (buffer.index < buffers.Length)
            {
                var source = new Span<byte>((void*)buffers[buffer.index], (int)buffer.bytesused);
                var span = writer.GetSpan(source.Length);
                source.CopyTo(span);
                writer.Advance(source.Length);
            }

            // Re-queue buffer
            var requeueBuffer = default(v4l2_buffer);
            requeueBuffer.index = buffer.index;
            requeueBuffer.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            requeueBuffer.memory = V4L2_MEMORY_MMAP;
            ioctl(fd, VIDIOC_QBUF, (IntPtr)(&requeueBuffer));
        }
        finally
        {
            ioctl(fd, VIDIOC_STREAMOFF, (IntPtr)(&type));
        }

        return true;
    }

    public bool StartCapture(int fps = 0)
    {
        lock (sync)
        {
            return StartCaptureCore(fps);
        }
    }

    private unsafe bool StartCaptureCore(int fps)
    {
        if (!IsOpen || IsCapturing)
        {
            return false;
        }

        if (fps > 0)
        {
            _ = SetFrameRateCore(fps);
        }

        if (!QueueAllBuffers())
        {
            return false;
        }

        // Start streaming
        var type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        if (ioctl(fd, VIDIOC_STREAMON, (IntPtr)(&type)) < 0)
        {
            return false;
        }

        // Start capture loop
        var source = new CancellationTokenSource();
        captureCts = source;
        captureThread = new Thread(() => CaptureLoop(fps, source.Token))
        {
            IsBackground = true,
            Name = "V4L2 Capture"
        };
        captureThread.Start();

        return true;
    }

    public bool StopCapture()
    {
        lock (sync)
        {
            return StopCaptureCore();
        }
    }

    private unsafe bool StopCaptureCore()
    {
        if ((captureCts is null) || (captureThread is null))
        {
            return true;
        }

        captureCts.Cancel();
        if (!captureThread.Join(StopTimeout))
        {
            return false;
        }

        captureThread = null;

        captureCts.Dispose();
        captureCts = null;

        // Stop streaming
        var type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        ioctl(fd, VIDIOC_STREAMOFF, (IntPtr)(&type));

        return true;
    }

    private unsafe void CaptureLoop(int fps, CancellationToken cancellationToken)
    {
        var frameInterval = fps > 0 ? TimeSpan.FromMilliseconds(1000.0 / fps) : TimeSpan.Zero;

        while (!cancellationToken.IsCancellationRequested)
        {
            var currentTimestamp = Stopwatch.GetTimestamp();

            var fds = new pollfd
            {
                fd = fd,
                events = POLLIN
            };
            if (poll(ref fds, 1, 100) <= 0)
            {
                continue;
            }

            // De-queue buffer
            var buffer = default(v4l2_buffer);
            buffer.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            buffer.memory = V4L2_MEMORY_MMAP;
            if (ioctl(fd, VIDIOC_DQBUF, (IntPtr)(&buffer)) < 0)
            {
                continue;
            }

            if (buffer.index < buffers.Length)
            {
                var handler = FrameCaptured;
                handler?.Invoke(new FrameBuffer(buffers[buffer.index], (int)buffer.bytesused));
            }

            // Re-queue buffer
            var requeueBuffer = default(v4l2_buffer);
            requeueBuffer.index = buffer.index;
            requeueBuffer.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            requeueBuffer.memory = V4L2_MEMORY_MMAP;
            ioctl(fd, VIDIOC_QBUF, (IntPtr)(&requeueBuffer));

            if (fps > 0)
            {
                var sleepTime = frameInterval - Stopwatch.GetElapsedTime(currentTimestamp);
                if (sleepTime > TimeSpan.Zero)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }
    }
}
