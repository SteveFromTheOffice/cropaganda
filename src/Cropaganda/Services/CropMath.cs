using System.Windows;

namespace Cropaganda.Services;

/// <summary>
/// Static helpers for 4:5 aspect ratio crop math.
/// All calculations are in source image pixel coordinates.
/// </summary>
public static class CropMath
{
    /// <summary>
    /// Returns the largest 4:5 Int32Rect that fits within the given image dimensions,
    /// centered on the image.
    /// 4:5 means width/height = 4/5, so height = width * 5/4.
    /// </summary>
    public static Int32Rect DefaultCropRect(int imageWidth, int imageHeight)
    {
        // The largest 4:5 rect that fits: constrain by whichever dimension is the bottleneck.
        // If we take full width: needed height = width * 5/4. If that exceeds imageHeight, width is the constraint.
        // If we take full height: needed width = height * 4/5. If that exceeds imageWidth, height is the constraint.
        int cropWidth = Math.Min(imageWidth, imageHeight * 4 / 5);
        int cropHeight = cropWidth * 5 / 4;

        // Ensure we never exceed bounds due to integer rounding
        cropHeight = Math.Min(cropHeight, imageHeight);

        int x = (imageWidth - cropWidth) / 2;
        int y = (imageHeight - cropHeight) / 2;

        return new Int32Rect(x, y, cropWidth, cropHeight);
    }

    /// <summary>
    /// Given a viewport pan offset and zoom scale, computes the Int32Rect in source image
    /// pixels that corresponds to the visible crop window.
    /// </summary>
    /// <param name="imageWidth">Source image width in pixels.</param>
    /// <param name="imageHeight">Source image height in pixels.</param>
    /// <param name="cropWindowPixelSize">On-screen size of the fixed crop box (e.g., 480×600).</param>
    /// <param name="panOffset">How much the image has been panned in screen pixels.</param>
    /// <param name="zoom">Scale factor: 1 image pixel = zoom screen pixels.</param>
    public static Int32Rect ComputeCropRect(
        int imageWidth, int imageHeight,
        Size cropWindowPixelSize,
        Vector panOffset,
        double zoom)
    {
        // The crop window's top-left in image coordinates:
        // screen x=0 of the crop window maps to image x = (0 - panOffset.X) / zoom
        double imgX = -panOffset.X / zoom;
        double imgY = -panOffset.Y / zoom;
        double imgW = cropWindowPixelSize.Width / zoom;
        double imgH = cropWindowPixelSize.Height / zoom;

        // Round to nearest integer
        int x = (int)Math.Round(imgX);
        int y = (int)Math.Round(imgY);
        int w = (int)Math.Round(imgW);
        int h = (int)Math.Round(imgH);

        // Clamp to image bounds
        x = Math.Clamp(x, 0, imageWidth - 1);
        y = Math.Clamp(y, 0, imageHeight - 1);
        w = Math.Clamp(w, 1, imageWidth - x);
        h = Math.Clamp(h, 1, imageHeight - y);

        return new Int32Rect(x, y, w, h);
    }
}
