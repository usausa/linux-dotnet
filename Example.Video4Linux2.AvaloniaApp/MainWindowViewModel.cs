namespace Example.Video4Linux2.AvaloniaApp;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using Example.Video4Linux2.AvaloniaApp.Components;
using Example.Video4Linux2.AvaloniaApp.Settings;

using LinuxDotNet.Video4Linux2;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public partial class MainWindowViewModel : ExtendViewModelBase
{
    private const float Alpha = 0.5f;

    private readonly IDispatcher dispatcher;

    private readonly CameraSetting cameraSetting;

    private readonly DetectSetting detectSetting;

    private readonly DispatcherTimer statusTimer;

    private readonly VideoCapture capture;

    private readonly FaceDetector faceDetector;

    private BufferManager? bufferManager;

    private WriteableBitmap? bitmap;

    private DateTime lastStatusAt;

    private int frameCount;
    private int lastGc0Count;
    private int lastGc1Count;
    private int lastGc2Count;

    private float previousFps;
    private float previousGc0PerSec;
    private float previousGc1PerSec;
    private float previousGc2PerSec;

    [ObservableProperty]
    public partial WriteableBitmap? Bitmap { get; set; }

    public ObservableCollection<FaceBox> FaceBoxes { get; } = new();

    [ObservableProperty]
    public partial float Fps { get; set; }

    [ObservableProperty]
    public partial float Gc0PerSec { get; set; }

    [ObservableProperty]
    public partial float Gc1PerSec { get; set; }

    [ObservableProperty]
    public partial float Gc2PerSec { get; set; }

    public IObserveCommand StartCommand { get; }

    public IObserveCommand StopCommand { get; }

    public MainWindowViewModel(
        IDispatcher dispatcher,
        CameraSetting cameraSetting,
        DetectSetting detectSetting)
    {
        this.dispatcher = dispatcher;
        this.cameraSetting = cameraSetting;
        this.detectSetting = detectSetting;

        statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        statusTimer.Tick += StatusTimerOnTick;

        capture = new VideoCapture(cameraSetting.Device);
        capture.FrameCaptured += CaptureOnFrameCaptured;

        faceDetector = new FaceDetector(detectSetting.Model, detectSetting.Parallel, detectSetting.IntraOpNumThreads, detectSetting.InterOpNumThreads);

        StartCommand = MakeDelegateCommand(StartCapture, () => !capture.IsCapturing);
        StopCommand = MakeDelegateCommand(StopCapture, () => capture.IsCapturing);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            statusTimer.Stop();
            statusTimer.Tick -= StatusTimerOnTick;

            capture.Dispose();
            capture.FrameCaptured -= CaptureOnFrameCaptured;

            faceDetector.Dispose();
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

        if (cameraSetting.Fps > 0)
        {
            capture.SetFrameRate(cameraSetting.Fps);
        }

        capture.StartCapture(cameraSetting.Fps);

        bufferManager ??= new BufferManager(4, capture.Width, capture.Height, 4);
        bitmap ??= new WriteableBitmap(new PixelSize(capture.Width, capture.Height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, AlphaFormat.Premul);

        frameCount = 0;
        lastGc0Count = GC.CollectionCount(0);
        lastGc1Count = GC.CollectionCount(1);
        lastGc2Count = GC.CollectionCount(2);
        lastStatusAt = DateTime.Now;

        statusTimer.Start();
    }

    private void StopCapture()
    {
        statusTimer.Stop();

        capture.StopCapture();
        capture.Close();
    }

    private void CaptureOnFrameCaptured(FrameBuffer frame)
    {
        if (bufferManager is null)
        {
            return;
        }

        var slot = bufferManager.NextSlot();
        lock (slot.Lock)
        {
            ImageHelper.ConvertYUYV2RGBA(frame.Span, slot.Buffer);

            if (detectSetting.Enable)
            {
                faceDetector.Detect(slot.Buffer, bufferManager.Width, bufferManager.Height);
                slot.FaceBoxes.Clear();
                slot.FaceBoxes.AddRange(faceDetector.DetectedFaceBoxes);
            }

            slot.MarkUpdated();
        }

        dispatcher.Post(UpdateBitmap);
    }

    private unsafe void UpdateBitmap()
    {
        if (IsDisposed || (bufferManager is null))
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
            var buffer = new Span<byte>(lockedBitmap.Address.ToPointer(), bufferManager.BufferSize);
            slot.Buffer.CopyTo(buffer);

            FaceBoxes.Clear();
            foreach (var box in slot.FaceBoxes)
            {
                FaceBoxes.Add(box);
            }
        }

        // Disable cache & update
        Bitmap = null;
        Bitmap = bitmap;

        frameCount++;
    }

    private void StatusTimerOnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var elapsed = (float)(now - lastStatusAt).TotalMilliseconds;
        lastStatusAt = now;

        // FPS
        var fps = (frameCount * 1000) / elapsed;
        frameCount = 0;
        previousFps = CalcSmoothValue(fps, previousFps);
        Fps = previousFps;

        // GC
        var gc0Count = GC.CollectionCount(0);
        var gc1Count = GC.CollectionCount(1);
        var gc2Count = GC.CollectionCount(2);

        var gc0PerSec = (gc0Count - lastGc0Count) * 1000 / elapsed;
        previousGc0PerSec = CalcSmoothValue(gc0PerSec, previousGc0PerSec);
        Gc0PerSec = previousGc0PerSec;

        var gc1PerSec = (gc1Count - lastGc1Count) * 1000 / elapsed;
        previousGc1PerSec = CalcSmoothValue(gc1PerSec, previousGc1PerSec);
        Gc1PerSec = previousGc1PerSec;

        var gc2PerSec = (gc2Count - lastGc2Count) * 1000 / elapsed;
        previousGc2PerSec = CalcSmoothValue(gc2PerSec, previousGc2PerSec);
        Gc2PerSec = previousGc2PerSec;

        lastGc0Count = gc0Count;
        lastGc1Count = gc1Count;
        lastGc2Count = gc2Count;

        static float CalcSmoothValue(float current, float previous)
        {
            return (current * Alpha) + (previous * (1.0f - Alpha));
        }
    }
}
