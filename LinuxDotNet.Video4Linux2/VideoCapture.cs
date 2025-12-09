namespace LinuxDotNet.Video4Linux2;

using System;
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
        var format = new v4l2_format
        {
            type = V4L2_BUF_TYPE_VIDEO_CAPTURE
        };

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
        // TODO
        //for (uint i = 0; i < requestBuffer.count; i++)
        //{
        //    var buffer = new v4l2_buffer
        //    {
        //        type = V4L2_BUF_TYPE_VIDEO_CAPTURE,
        //        memory = V4L2_MEMORY_MMAP,
        //        index = i
        //    };

        //    var bufferPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buffer));
        //    try
        //    {
        //        Marshal.StructureToPtr(buffer, bufferPtr, false);
        //        if (ioctl(fd, VIDIOC_QUERYBUF, bufferPtr) < 0)
        //        {
        //            Close();
        //            return false;
        //        }

        //        buffer = Marshal.PtrToStructure<v4l2_buffer>(bufferPtr);
        //    }
        //    finally
        //    {
        //        Marshal.FreeHGlobal(bufferPtr);
        //    }

        //    buffers[i] = mmap(IntPtr.Zero, (int)buffer.length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, (int)buffer.offset);
        //    bufferLengths[i] = (int)buffer.length;

        //    if (buffers[i] == new IntPtr(-1))
        //    {
        //        Close();
        //        return false;
        //    }
        //}

        // TODO

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
