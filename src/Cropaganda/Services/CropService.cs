using System;
using System.IO;
using SkiaSharp;

namespace Cropaganda.Services;

public class CropService : ICropService
{
    public SKBitmap LoadImage(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"Image file not found: {path}");

        try
        {
            var bitmap = SKBitmap.Decode(path);
            if (bitmap == null)
                throw new InvalidOperationException($"Failed to decode image '{path}': file may be corrupt or unsupported.");
            return bitmap;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to load image '{path}': {ex.Message}", ex);
        }
    }

    public SKBitmap Crop(SKBitmap source, SKRectI cropRect)
    {
        int x      = Math.Max(0, cropRect.Left);
        int y      = Math.Max(0, cropRect.Top);
        int right  = Math.Min(source.Width,  cropRect.Right);
        int bottom = Math.Min(source.Height, cropRect.Bottom);
        int width  = right - x;
        int height = bottom - y;

        if (width <= 0 || height <= 0)
            throw new ArgumentException(
                $"cropRect results in zero or negative area after clamping to {source.Width}x{source.Height}.");

        var dest   = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(dest);
        canvas.DrawBitmap(source, new SKRectI(x, y, x + width, y + height),
                          new SKRect(0, 0, width, height));
        return dest;
    }

    public void Save(SKBitmap image, string outputPath, int jpegQuality = 95)
    {
        int quality = Math.Clamp(jpegQuality, 1, 100);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        encoded.SaveTo(stream);
    }
}
