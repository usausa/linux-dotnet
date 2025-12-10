namespace LinuxDotNet.Video4Linux2;

using System;
using System.Buffers;
using System.Runtime.Versioning;

using static LinuxDotNet.Video4Linux2.NativeMethods;

// TODO
#pragma warning disable CA1806
[SupportedOSPlatform("linux")]
public sealed class VideoCapture : IDisposable
{
    //public event Action<byte[], int, int>? FrameCaptured;

    private readonly string path;

    private int fd = -1;

    private IntPtr[] buffers = [];

    private int[] bufferLengths = [];

    private Thread? captureThread;

    private CancellationTokenSource? captureCts;

    private byte[] frameBuffer = [];

    public int Width { get; private set; }

    public int Height { get; private set; }

    public bool IsOpen => fd >= 0;

    public bool IsCapturing => captureCts is { IsCancellationRequested: false };

    public VideoCapture(string path)
    {
        this.path = path;
    }

    public void Dispose()
    {
        Close();
    }

    public unsafe bool Open(int width = 640, int height = 480)
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
        v4l2_format format;
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
            v4l2_buffer buffer;
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

            // Queue buffer
            v4l2_buffer buffer2;
            buffer2.index = i;
            buffer2.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            buffer2.memory = V4L2_MEMORY_MMAP;

            if (ioctl(fd, VIDIOC_QBUF, (IntPtr)(&buffer2)) < 0)
            {
                CloseInternal();
                return false;
            }
        }

        var maxSize = bufferLengths.Max();
        frameBuffer = ArrayPool<byte>.Shared.Rent(maxSize);

        return true;
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        StopCapture();
        CloseInternal();
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

        if (frameBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(frameBuffer);
            frameBuffer = [];
        }

        Width = 0;
        Height = 0;
    }

    public unsafe bool StartCapture()
    {
        if (!IsOpen || IsCapturing)
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
        captureCts = new CancellationTokenSource();
        captureThread = new Thread(() => CaptureLoop(captureCts.Token))
        {
            IsBackground = true,
            Name = "V4L2 Capture"
        };
        captureThread.Start();

        return false;
    }

    public unsafe void StopCapture()
    {
        if (!IsCapturing)
        {
            return;
        }

        captureCts?.Cancel();
        captureThread?.Join(2000);
        captureThread = null;

        captureCts?.Dispose();
        captureCts = null;

        // Stop streaming
        var type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        ioctl(fd, VIDIOC_STREAMOFF, (IntPtr)(&type));
    }

    private unsafe void CaptureLoop(CancellationToken cancellationToken)
    {
        // TODO
    }
}
