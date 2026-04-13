using SkiaSharp;

namespace Cropaganda.Services;

/// <summary>
/// Core image cropping operations. Uses SkiaSharp for cross-platform support.
/// </summary>
public interface ICropService
{
    /// <summary>Loads an image from disk.</summary>
    SKBitmap LoadImage(string path);

    /// <summary>Crops the source image to the specified pixel rectangle.</summary>
    SKBitmap Crop(SKBitmap source, SKRectI cropRect);

    /// <summary>Saves the image as JPEG with the specified quality (1-100).</summary>
    void Save(SKBitmap image, string outputPath, int jpegQuality = 95);
}
