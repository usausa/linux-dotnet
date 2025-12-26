namespace Example.Video4Linux2.AvaloniaApp;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using Example.Video4Linux2.AvaloniaApp.Helper;
using Example.Video4Linux2.AvaloniaApp.Settings;

using LinuxDotNet.Video4Linux2;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public partial class MainWindowViewModel : ExtendViewModelBase
{
    //private const int BitmapBufferSize = Width * Height * 4;

    private readonly IDispatcher dispatcher;

    private readonly CameraSetting cameraSetting;

    private readonly VideoCapture capture;

    private BufferManager? bufferManager;

    private WriteableBitmap? bitmap;

    private int bitmapBufferSize;

    [ObservableProperty]
    public partial WriteableBitmap? Bitmap { get; set; }

    public IObserveCommand StartCommand { get; }

    public IObserveCommand StopCommand { get; }

    public MainWindowViewModel(
        IDispatcher dispatcher,
        CameraSetting cameraSetting)
    {
        this.dispatcher = dispatcher;
        this.cameraSetting = cameraSetting;

        capture = new VideoCapture(cameraSetting.Device);
        capture.FrameCaptured += CaptureOnFrameCaptured;

        StartCommand = MakeDelegateCommand(StartCapture, () => !capture.IsCapturing);
        StopCommand = MakeDelegateCommand(StopCapture, () => capture.IsCapturing);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            capture.Dispose();
            capture.FrameCaptured -= CaptureOnFrameCaptured;

            bufferManager?.Dispose();
            bitmap?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void StartCapture()
    {
        if (!capture.Open(cameraSetting.Width, cameraSetting.Height))
        {
            return;
        }
        capture.StartCapture();

        bitmapBufferSize = capture.Width * capture.Height * 4;
        bufferManager ??= new BufferManager(4, bitmapBufferSize);
        bitmap ??= new WriteableBitmap(new PixelSize(capture.Width, capture.Height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, AlphaFormat.Premul);
    }

    private void StopCapture()
    {
        capture.StopCapture();
        capture.Close();
    }

    private void CaptureOnFrameCaptured(FrameBuffer frame)
    {
        if (bufferManager is null)
        {
            return;
        }

        // TODO show fps
        var slot = bufferManager.NextSlot();
        lock (slot.Lock)
        {
            ImageHelper.ConvertYUYV2RGBA(frame.Span, slot.Buffer);
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

        var slot = bufferManager?.LastUpdateSlot();
        if (slot is null)
        {
            return;
        }

        // Lock
        lock (slot.Lock)
        {
            using var lockedBitmap = bitmap!.Lock();
            var buffer = new Span<byte>(lockedBitmap.Address.ToPointer(), bitmapBufferSize);
            slot.Buffer.CopyTo(buffer);
        }

        // Disable cache & update
        Bitmap = null;
        Bitmap = bitmap;
    }
}
