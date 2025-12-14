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
    public partial WriteableBitmap? Bitmap { set; get; }

    public IObserveCommand StartCommand { get; }

    public IObserveCommand StopCommand { get; }

    public MainWindowViewModel(IDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
        bufferManager = new BufferManager(4, BitmapBufferSize);
        capture = new VideoCapture("/dev/video0");
        capture.FrameCaptured += CaptureOnFrameCaptured;
        bitmap = new WriteableBitmap(new PixelSize(Width, Height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, AlphaFormat.Premul);

        //StartCommand = MakeDelegateCommand(StartCapture, () => !capture.IsCapturing);
        //StopCommand = MakeDelegateCommand(StopCapture, () => capture.IsCapturing);
        StartCommand = MakeDelegateCommand(StartCapture, () => thread is null);
        StopCommand = MakeDelegateCommand(StopCapture, () => thread is not null);
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

    // TODO delete
#pragma warning disable CA2213
    private Thread? thread;
    private CancellationTokenSource? cts;
    private int counter;
#pragma warning restore CA2213

    private void StartCapture()
    {
        cts = new CancellationTokenSource();
        thread = new Thread(x =>
        {
            var token = ((CancellationTokenSource)x!).Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    CaptureOnFrameCaptured(default!);
                    Thread.Sleep(16);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        })
        {
            IsBackground = true
        };
        thread.Start(cts);

        // TODO buffer size fix
        //capture.Open
        //capture.StartCapture();
    }

    private void StopCapture()
    {
        // TODO stop camera
        //capture.StopCapture();
        //capture.Close();

        cts?.Cancel();
        thread?.Join();
        thread = null;
    }

    private void CaptureOnFrameCaptured(FrameBuffer frame)
    {
        // TODO convert image
        var slot = bufferManager.NextSlot();
        lock (slot.Lock)
        {
            counter++;
            if (counter > Width * Height)
            {
                counter = 0;
            }

            slot.Buffer.Fill(255);
            for (var i = 0; i < counter; i++)
            {
                var pixel = slot.Buffer.Slice(i * 4, 4);
                pixel[0] = 0;
                pixel[1] = 0;
                pixel[2] = 0;
            }

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
