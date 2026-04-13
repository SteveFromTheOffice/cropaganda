using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Cropaganda.Services;

public class CropService : ICropService
{
    public BitmapSource LoadImage(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"Image file not found: {path}");

        try
        {
            var decoder = BitmapDecoder.Create(
                new Uri(path, UriKind.Absolute),
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            var bitmap = decoder.Frames[0];
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to load image '{path}': {ex.Message}", ex);
        }
    }

    public BitmapSource Crop(BitmapSource source, Int32Rect cropRect)
    {
        // Clamp cropRect to source bounds
        int x = Math.Max(0, cropRect.X);
        int y = Math.Max(0, cropRect.Y);
        int right = Math.Min(source.PixelWidth, cropRect.X + cropRect.Width);
        int bottom = Math.Min(source.PixelHeight, cropRect.Y + cropRect.Height);
        int width = right - x;
        int height = bottom - y;

        if (width <= 0 || height <= 0)
            throw new ArgumentException(
                $"cropRect {cropRect} results in zero or negative area after clamping to {source.PixelWidth}x{source.PixelHeight}.");

        var clamped = new Int32Rect(x, y, width, height);
        var cropped = new CroppedBitmap(source, clamped);
        cropped.Freeze();
        return cropped;
    }

    public void Save(BitmapSource image, string outputPath, int jpegQuality = 95, BitmapMetadata? metadata = null)
    {
        int quality = Math.Clamp(jpegQuality, 1, 100);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var encoder = new JpegBitmapEncoder
        {
            QualityLevel = quality
        };

        BitmapFrame frame;
        if (metadata != null)
        {
            var metadataClone = metadata.Clone();
            frame = BitmapFrame.Create(image, null, metadataClone, null);
        }
        else
        {
            frame = BitmapFrame.Create(image);
        }

        encoder.Frames.Add(frame);

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        encoder.Save(stream);
    }
}
