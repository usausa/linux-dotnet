namespace Example.GameInput.AvaloniaApp.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using SkiaSharp;

public sealed class SpeedGauge : Control
{
    public static readonly StyledProperty<int> SpeedProperty = AvaloniaProperty.Register<SpeedGauge, int>(nameof(Speed));

    public static readonly StyledProperty<int> MaxSpeedProperty = AvaloniaProperty.Register<SpeedGauge, int>(nameof(MaxSpeed), 255);

    public static readonly StyledProperty<int> GaugeWidthProperty = AvaloniaProperty.Register<SpeedGauge, int>(nameof(GaugeWidth), 32);

    public static readonly StyledProperty<Color> BackgroundColorProperty = AvaloniaProperty.Register<SpeedGauge, Color>(nameof(BackgroundColor), Colors.Black);

    public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<SpeedGauge, Color>(nameof(Color), Colors.LightGreen);

    public int Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public int MaxSpeed
    {
        get => GetValue(MaxSpeedProperty);
        set => SetValue(MaxSpeedProperty, value);
    }

    public int GaugeWidth
    {
        get => GetValue(GaugeWidthProperty);
        set => SetValue(GaugeWidthProperty, value);
    }

    public Color BackgroundColor
    {
        get => GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public SpeedGauge()
    {
        AffectsRender<SpeedGauge>(SpeedProperty);
        AffectsRender<SpeedGauge>(MaxSpeedProperty);
        AffectsRender<SpeedGauge>(GaugeWidthProperty);
        AffectsRender<SpeedGauge>(BackgroundColorProperty);
        AffectsRender<SpeedGauge>(ColorProperty);
    }

    public override void Render(DrawingContext context)
    {
        using var op = new ShapeCustomDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            (float)Speed / MaxSpeed,
            GaugeWidth,
            BackgroundColor.ToSKColor(),
            Color.ToSKColor());
        context.Custom(op);
    }

    private sealed class ShapeCustomDrawOperation : ICustomDrawOperation
    {
        private readonly float value;

        private readonly float strokeWidth;

        private readonly SKColor backgroundColor;

        private readonly SKColor color;

        public Rect Bounds { get; }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public ShapeCustomDrawOperation(Rect bounds, float value, float strokeWidth, SKColor backgroundColor, SKColor color)
        {
            Bounds = bounds;
            this.value = value;
            this.strokeWidth = strokeWidth;
            this.backgroundColor = backgroundColor;
            this.color = color;
        }

        public void Dispose()
        {
        }

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            var centerX = (float)(Bounds.Width / 2);
            var centerY = (float)(Bounds.Height / 2);

            var arcSize = (float)Math.Min(Bounds.Width, Bounds.Height);
            var arcRadius = arcSize / 2;
            var strokeMargin = strokeWidth / 2;
            var meterRect = new SKRect(
                centerX - arcRadius + strokeMargin,
                centerY - arcRadius + strokeMargin,
                centerX + arcRadius - strokeMargin,
                centerY + arcRadius - strokeMargin);

            const float startAngle = -210f;
            const float backgroundAngle = 240f;
            var valueAngle = backgroundAngle * value;

            using var path = new SKPath();

            // Draw background
            using var backgroundPaint = new SKPaint();
            backgroundPaint.Style = SKPaintStyle.Stroke;
            backgroundPaint.Color = backgroundColor;
            backgroundPaint.StrokeWidth = strokeWidth;
            backgroundPaint.IsAntialias = true;

            path.AddArc(meterRect, startAngle, backgroundAngle);
            canvas.DrawPath(path, backgroundPaint);

            // Draw value
            path.Reset();

            using var valuePaint = new SKPaint();
            valuePaint.Style = SKPaintStyle.Stroke;
            valuePaint.Color = color;
            valuePaint.StrokeWidth = strokeWidth;
            valuePaint.IsAntialias = true;

            path.AddArc(meterRect, startAngle, valueAngle);
            canvas.DrawPath(path, valuePaint);
        }
    }
}
