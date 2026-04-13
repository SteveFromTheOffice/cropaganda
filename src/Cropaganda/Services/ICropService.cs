using System.Windows;
using System.Windows.Media.Imaging;

namespace Cropaganda.Services;

/// <summary>
/// Core image cropping operations. Uses WPF's built-in imaging (WIC).
/// </summary>
public interface ICropService
{
    /// <summary>
    /// Loads an image from disk, preserving EXIF metadata.
    /// </summary>
    BitmapSource LoadImage(string path);

    /// <summary>
    /// Crops the source image to the specified rectangle (in pixel coordinates).
    /// </summary>
    BitmapSource Crop(BitmapSource source, Int32Rect cropRect);

    /// <summary>
    /// Saves the image as JPEG with the specified quality (1-100).
    /// Preserves original EXIF metadata when available.
    /// </summary>
    void Save(BitmapSource image, string outputPath, int jpegQuality = 95, BitmapMetadata? metadata = null);
}
