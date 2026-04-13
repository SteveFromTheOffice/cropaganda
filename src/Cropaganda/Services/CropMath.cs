using Avalonia;
using SkiaSharp;

namespace Cropaganda.Services;

public static class CropMath
{
    private const double CropAspectRatio = 4.0 / 5.0;

    /// <summary>
    /// Returns the largest centered 4:5 rectangle that fits within the image dimensions.
    /// </summary>
    public static SKRectI DefaultCropRect(int imageWidth, int imageHeight)
    {
        double aspectW = imageHeight * CropAspectRatio;
        int cropWidth, cropHeight;

        if (aspectW <= imageWidth)
        {
            cropWidth  = (int)Math.Round(aspectW);
            cropHeight = imageHeight;
        }
        else
        {
            cropWidth  = imageWidth;
            cropHeight = (int)Math.Round(imageWidth / CropAspectRatio);
        }

        int x = (imageWidth  - cropWidth)  / 2;
        int y = (imageHeight - cropHeight) / 2;

        return SKRectI.Create(x, y, cropWidth, cropHeight);
    }

    /// <summary>
    /// Converts viewport pan/zoom state to an image-pixel crop rectangle.
    /// cropWindowPixelSize = size of the crop overlay in display pixels.
    /// panOffset = how far (in display pixels) the image has been panned;
    ///   positive X pans the image right, shifting the crop window left in image space.
    /// zoom = display scale factor (1.0 = 1 display pixel per image pixel).
    /// </summary>
    public static SKRectI ComputeCropRect(
        int imageWidth, int imageHeight,
        Size cropWindowPixelSize, Vector panOffset, double zoom)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || zoom <= 0)
            return SKRectI.Empty;

        double x = -panOffset.X / zoom;
        double y = -panOffset.Y / zoom;
        double w = cropWindowPixelSize.Width  / zoom;
        double h = cropWindowPixelSize.Height / zoom;

        double x2 = Math.Min(x + w, imageWidth);
        double y2 = Math.Min(y + h, imageHeight);
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        w = x2 - x;
        h = y2 - y;

        if (w <= 0 || h <= 0) return SKRectI.Empty;

        return SKRectI.Create(
            (int)Math.Round(x), (int)Math.Round(y),
            (int)Math.Round(w), (int)Math.Round(h));
    }
}
