namespace Example.Video4Linux2.AvaloniaApp;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using Example.Video4Linux2.AvaloniaApp.Helper;

using LinuxDotNet.Video4Linux2;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public partial class MainWindowViewModel : ExtendViewModelBase
{
    private const int Width = 640;
    private const int Height = 480;
    private const int BitmapBufferSize = Width * Height * 4;

    private readonly IDispatcher dispatcher;

    private readonly BufferManager bufferManager;

    private readonly VideoCapture capture;

    private readonly WriteableBitmap? bitmap;

    [ObservableProperty]
    public partial WriteableBitmap? Bitmap { get; set; }

    public IObserveCommand StartCommand { get; }

    public IObserveCommand StopCommand { get; }

    public MainWindowViewModel(IDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
        bufferManager = new BufferManager(4, BitmapBufferSize);
        capture = new VideoCapture("/dev/video0");
        capture.FrameCaptured += CaptureOnFrameCaptured;
        bitmap = new WriteableBitmap(new PixelSize(Width, Height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, AlphaFormat.Premul);

        StartCommand = MakeDelegateCommand(StartCapture, () => !capture.IsCapturing);
        StopCommand = MakeDelegateCommand(StopCapture, () => capture.IsCapturing);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            capture.Dispose();
            capture.FrameCaptured -= CaptureOnFrameCaptured;
            bufferManager.Dispose();
            bitmap?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void StartCapture()
    {
        // TODO fix size
        capture.Open();
        capture.StartCapture();
    }

    private void StopCapture()
    {
        capture.StopCapture();
        capture.Close();
    }

    private void CaptureOnFrameCaptured(FrameBuffer frame)
    {
        // TODO show fps
        var slot = bufferManager.NextSlot();
        lock (slot.Lock)
        {
            ImageHelper.ConvertYUYV2RGBA(frame.AsSpan(), slot.Buffer);
            slot.MarkUpdated();
        }

        dispatcher.Post(UpdateBitmap);
    }

    private unsafe void UpdateBitmap()
    {
        if (IsDisposed)
        {
            return;
        }

        var slot = bufferManager.LastUpdateSlot();
        if (slot is null)
        {
            return;
        }

        // Lock
        lock (slot.Lock)
        {
            using var lockedBitmap = bitmap!.Lock();
            var buffer = new Span<byte>(lockedBitmap.Address.ToPointer(), BitmapBufferSize);
            slot.Buffer.CopyTo(buffer);
        }

        // Disable cache & update
        Bitmap = null;
        Bitmap = bitmap;
    }
}
