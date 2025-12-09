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

    public bool Open(int width = 640, int height = 480)
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

        //var cap = default(v4l2_capability);
        //var capPtr = Marshal.AllocHGlobal(Marshal.SizeOf(cap));
        //Marshal.StructureToPtr(cap, capPtr, false);
        //if (ioctl(fd, VIDIOC_QUERYCAP, capPtr) < 0)
        //{
        //    Marshal.FreeHGlobal(capPtr);
        //    Close();
        //    return false;
        //}

        //Marshal.FreeHGlobal(capPtr);

        // Set format
        var format = new v4l2_format
        {
            type = V4L2_BUF_TYPE_VIDEO_CAPTURE,
            fmt = new v4l2_pix_format
            {
                width = (uint)width,
                height = (uint)height,
                pixelformat = V4L2_PIX_FMT_YUYV,
                field = V4L2_FIELD_NONE
            }
        };

        var fmtPtr = Marshal.AllocHGlobal(Marshal.SizeOf(format));
        try
        {
            Marshal.StructureToPtr(format, fmtPtr, false);
            if (ioctl(fd, VIDIOC_S_FMT, fmtPtr) < 0)
            {
                Close();
                return false;
            }

            format = Marshal.PtrToStructure<v4l2_format>(fmtPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(fmtPtr);
        }

        Width = (int)format.fmt.width;
        Height = (int)format.fmt.height;

        //// Request buffers
        //var requestBuffer = new v4l2_requestbuffers
        //{
        //    count = 4,
        //    type = V4L2_BUF_TYPE_VIDEO_CAPTURE,
        //    memory = V4L2_MEMORY_MMAP,
        //    reserved = new uint[2]
        //};

        //var requestBufferPtr = Marshal.AllocHGlobal(Marshal.SizeOf(requestBuffer));
        //try
        //{
        //    Marshal.StructureToPtr(requestBuffer, requestBufferPtr, false);
        //    if (ioctl(fd, VIDIOC_REQBUFS, requestBufferPtr) < 0)
        //    {
        //        Close();
        //        return false;
        //    }

        //    requestBuffer = Marshal.PtrToStructure<v4l2_requestbuffers>(requestBufferPtr);
        //}
        //finally
        //{
        //    Marshal.FreeHGlobal(requestBufferPtr);
        //}

        //buffers = new IntPtr[requestBuffer.count];
        //bufferLengths = new int[requestBuffer.count];

        //// Map buffers
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
        if (IsOpen)
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
