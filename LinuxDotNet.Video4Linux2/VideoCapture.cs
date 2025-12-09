namespace LinuxDotNet.Video4Linux2;

using System;
using System.Runtime.InteropServices;
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

    // TODO

    public int Width { get; private set; }

    public int Height { get; private set; }

    public bool IsOpen => fd >= 0;

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

        fd = NativeMethods.open(path, NativeMethods.O_RDWR);
        if (fd < 0)
        {
            return false;
        }

        // Set format
        v4l2_format format;
        format.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;

        var pix = (v4l2_pix_format*)format.data;
        pix->width = (uint)width;
        pix->height = (uint)height;
        pix->pixelformat = V4L2_PIX_FMT_YUYV;
        pix->field = V4L2_FIELD_NONE;

        if (ioctl(fd, VIDIOC_S_FMT, (IntPtr)(&format)) < 0)
        {
            Close();
            return false;
        }

        Width = (int)pix->width;
        Height = (int)pix->height;

        // Request buffers
        v4l2_requestbuffers requestBuffers;
        requestBuffers.count = 4;
        requestBuffers.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
        requestBuffers.memory = V4L2_MEMORY_MMAP;
        requestBuffers.reserved[0] = 0;
        requestBuffers.reserved[1] = 0;

        if (ioctl(fd, VIDIOC_REQBUFS, (IntPtr)(&requestBuffers)) < 0)
        {
            Close();
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
                Console.WriteLine("*1 " + Marshal.GetLastWin32Error());
                Close();
                return false;
            }

            // Memory map
            buffers[i] = mmap(IntPtr.Zero, (int)buffer.length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, (int)buffer.offset);
            bufferLengths[i] = (int)buffer.length;

            // Queue buffer
            v4l2_buffer buffer2;
            buffer2.index = i;
            buffer2.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
            buffer2.memory = V4L2_MEMORY_MMAP;

            if (ioctl(fd, VIDIOC_QBUF, (IntPtr)(&buffer2)) < 0)
            {
                Close();
                return false;
            }
        }

        return true;
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        // TODO
        //StopCapture();

        for (var i = 0; i < buffers.Length; i++)
        {
            if ((buffers[i] != IntPtr.Zero) && (buffers[i] != new IntPtr(-1)))
            {
                munmap(buffers[i], bufferLengths[i]);
            }

            buffers[i] = IntPtr.Zero;
        }

        Width = 0;
        Height = 0;

        close(fd);
        fd = -1;
    }

    //public bool StartCapture()
    //{
    //    // TODO
    //    return false;
    //}

    //public void StopCapture()
    //{
    //    // TODO
    //}
}
