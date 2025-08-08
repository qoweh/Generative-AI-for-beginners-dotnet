using SkiaSharp;

namespace MySpaceAINet.Screenshot;

public static class ScreenshotService
{
    private static string _dir = Path.Combine(Environment.CurrentDirectory, "screenshoots");
    private static DateTime _lastAuto = DateTime.MinValue;

    public static void Init()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        Directory.CreateDirectory(_dir);
    }

    public static void MaybeAutoCapture(RenderState state)
    {
        if ((DateTime.UtcNow - _lastAuto).TotalSeconds > 5)
        {
            _lastAuto = DateTime.UtcNow;
            SaveScreenshot(state);
        }
    }

    public static void SaveScreenshot(RenderState state)
    {
        int cellW = 12; // zoom for readability
        int cellH = 20;
        int imgW = state.Width * cellW;
        int imgH = state.Height * cellH;

        using var bitmap = new SKBitmap(imgW, imgH);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 16,
            Typeface = SKTypeface.FromFamilyName("Menlo", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                var ch = state.Chars[y, x];
                var color = ConvertColor(state.Colors[y, x]);
                paint.Color = color;
                // Position baseline: approx 0.75 of cell height down
                canvas.DrawText(ch.ToString(), x * cellW, y * cellH + (int)(cellH * 0.75), paint);
            }
        }

        var path = Path.Combine(_dir, $"shot_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.png");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.OpenWrite(path);
        data.SaveTo(fs);
    }

    private static SKColor ConvertColor(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => SKColors.Black,
        ConsoleColor.DarkBlue => SKColors.DarkBlue,
        ConsoleColor.DarkGreen => SKColors.DarkGreen,
        ConsoleColor.DarkCyan => SKColors.Teal,
        ConsoleColor.DarkRed => SKColors.Maroon,
        ConsoleColor.DarkMagenta => SKColors.Purple,
        ConsoleColor.DarkYellow => new SKColor(184, 134, 11),
        ConsoleColor.Gray => SKColors.Gray,
        ConsoleColor.DarkGray => SKColors.DarkGray,
        ConsoleColor.Blue => SKColors.Blue,
        ConsoleColor.Green => SKColors.Green,
        ConsoleColor.Cyan => SKColors.Cyan,
        ConsoleColor.Red => SKColors.Red,
        ConsoleColor.Magenta => SKColors.Magenta,
        ConsoleColor.Yellow => SKColors.Yellow,
        ConsoleColor.White => SKColors.White,
        _ => SKColors.White
    };
}
