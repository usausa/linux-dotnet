namespace Example.Cups;

using SkiaSharp;

internal static class SampleImage
{
    public static MemoryStream Create()
    {
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);

        // Background
        canvas.Clear(SKColors.White);

        // Title
        using var titlePaint = new SKPaint();
        titlePaint.Color = SKColors.Black;
        titlePaint.IsAntialias = true;
        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 48);
        canvas.DrawText("CUPS API Test", 50, 80, titleFont, titlePaint);

        // DateTime
        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.DarkGray;
        textPaint.IsAntialias = true;
        using var textFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 24);
        canvas.DrawText($"DateTime: {DateTime.Now:yyyy/MM/dd HH:mm:ss}", 50, 130, textFont, textPaint);

        // Rectangle and Circle
        using var rectPaint = new SKPaint();
        rectPaint.Color = SKColors.Blue;
        rectPaint.Style = SKPaintStyle.Fill;
        canvas.DrawRect(50, 180, 300, 150, rectPaint);

        using var circlePaint = new SKPaint();
        circlePaint.Color = SKColors.Red;
        circlePaint.Style = SKPaintStyle.Stroke;
        circlePaint.StrokeWidth = 8;
        canvas.DrawCircle(550, 350, 100, circlePaint);

        // Gradation
        using var gradientPaint = new SKPaint();
        gradientPaint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(50, 400),
            new SKPoint(750, 550),
            [SKColors.Green, SKColors.Yellow, SKColors.Orange],
            null,
            SKShaderTileMode.Clamp);
        canvas.DrawRect(50, 400, 700, 150, gradientPaint);

        // PNG as Stream
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }
}
