namespace Example.Video4Linux2.AvaloniaApp.Controls;

using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Media;

using Example.Video4Linux2.AvaloniaApp.Components;

public class FaceBoxOverlay : Avalonia.Controls.Control
{
    public static readonly StyledProperty<ObservableCollection<FaceBox>?> FaceBoxesProperty =
        AvaloniaProperty.Register<FaceBoxOverlay, ObservableCollection<FaceBox>?>(nameof(FaceBoxes));

    public static readonly StyledProperty<Color> LowScoreColorProperty =
        AvaloniaProperty.Register<FaceBoxOverlay, Color>(nameof(LowScoreColor), Colors.Yellow);

    public static readonly StyledProperty<Color> HighScoreColorProperty =
        AvaloniaProperty.Register<FaceBoxOverlay, Color>(nameof(HighScoreColor), Colors.Red);

#pragma warning disable CA2227
    public ObservableCollection<FaceBox>? FaceBoxes
    {
        get => GetValue(FaceBoxesProperty);
        set => SetValue(FaceBoxesProperty, value);
    }
#pragma warning restore CA2227

    public Color LowScoreColor
    {
        get => GetValue(LowScoreColorProperty);
        set => SetValue(LowScoreColorProperty, value);
    }

    public Color HighScoreColor
    {
        get => GetValue(HighScoreColorProperty);
        set => SetValue(HighScoreColorProperty, value);
    }

    static FaceBoxOverlay()
    {
        AffectsRender<FaceBoxOverlay>(FaceBoxesProperty, LowScoreColorProperty, HighScoreColorProperty);
    }

    public FaceBoxOverlay()
    {
        ClipToBounds = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FaceBoxesProperty)
        {
            if (change.OldValue is ObservableCollection<FaceBox> oldCollection)
            {
                oldCollection.CollectionChanged -= OnFaceBoxesCollectionChanged;
            }

            if (change.NewValue is ObservableCollection<FaceBox> newCollection)
            {
                newCollection.CollectionChanged += OnFaceBoxesCollectionChanged;
            }
        }
    }

    private void OnFaceBoxesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if ((FaceBoxes is null) || (FaceBoxes.Count == 0))
        {
            return;
        }

        var scaleX = Bounds.Width;
        var scaleY = Bounds.Height;

        foreach (var faceBox in FaceBoxes)
        {
            var left = faceBox.Left * scaleX;
            var top = faceBox.Top * scaleY;
            var right = faceBox.Right * scaleX;
            var bottom = faceBox.Bottom * scaleY;

            var rect = new Rect(left, top, right - left, bottom - top);

            var color = InterpolateColor(LowScoreColor, HighScoreColor, faceBox.Confidence);
            var pen = new Pen(new SolidColorBrush(color), 2);

            context.DrawRectangle(null, pen, rect);

            var confidenceText = $"{faceBox.Confidence:P1}";
            var formattedText = new FormattedText(
                confidenceText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                12,
                new SolidColorBrush(color));

            var textX = right - formattedText.Width - 4;
            var textY = bottom - formattedText.Height - 4;

            context.DrawText(formattedText, new Point(textX, textY));
        }
    }

    private static Color InterpolateColor(Color lowColor, Color highColor, float confidence)
    {
        var r = (byte)(lowColor.R + ((highColor.R - lowColor.R) * confidence));
        var g = (byte)(lowColor.G + ((highColor.G - lowColor.G) * confidence));
        var b = (byte)(lowColor.B + ((highColor.B - lowColor.B) * confidence));
        var a = (byte)(lowColor.A + ((highColor.A - lowColor.A) * confidence));
        return Color.FromArgb(a, r, g, b);
    }
}
