using Avalonia;
using SkiaSharp;
using Xunit;
using Cropaganda.Services;

namespace Cropaganda.Tests;

/// <summary>
/// Tests for CropMath — the static math helper that computes 4:5 crop rectangles.
/// Assumes: CropMath.DefaultCropRect(int width, int height) -> SKRectI
///          CropMath.ComputeCropRect(int imageW, int imageH, Size cropWindowPixelSize, Vector panOffset, double zoom) -> SKRectI
/// </summary>
public class CropMathTests
{
    // ── DefaultCropRect ──────────────────────────────────────────────────────

    [Fact]
    public void DefaultCropRect_Landscape1920x1080_IsHeightLimited864x1080()
    {
        // 1080 * (4/5) = 864; image is wider than 4:5, so height is the limiting dimension.
        var rect = CropMath.DefaultCropRect(1920, 1080);

        Assert.Equal(864, rect.Width);
        Assert.Equal(1080, rect.Height);
    }

    [Fact]
    public void DefaultCropRect_Landscape1920x1080_IsCenteredHorizontally()
    {
        var rect = CropMath.DefaultCropRect(1920, 1080);

        int expectedX = (1920 - 864) / 2; // 528
        Assert.InRange(rect.Left, expectedX - 1, expectedX + 1);
        Assert.Equal(0, rect.Top);
    }

    [Fact]
    public void DefaultCropRect_Portrait1080x1350_FitsFourToFiveRatio()
    {
        // 1080/1350 = 0.8 = 4/5 exactly → full image
        var rect = CropMath.DefaultCropRect(1080, 1350);

        double ratio = (double)rect.Width / rect.Height;
        Assert.Equal(0.8, ratio, precision: 2);
    }

    [Fact]
    public void DefaultCropRect_Square1000x1000_Is800x1000()
    {
        // Height-limited: width = 1000 * 4/5 = 800
        var rect = CropMath.DefaultCropRect(1000, 1000);

        Assert.Equal(800, rect.Width);
        Assert.Equal(1000, rect.Height);
    }

    [Fact]
    public void DefaultCropRect_Exact4to5Image800x1000_IsFullImage()
    {
        var rect = CropMath.DefaultCropRect(800, 1000);

        Assert.Equal(800, rect.Width);
        Assert.Equal(1000, rect.Height);
        Assert.Equal(0, rect.Left);
        Assert.Equal(0, rect.Top);
    }

    [Fact]
    public void DefaultCropRect_VerySmall10x10_ReturnsValidRect()
    {
        // 10 * 4/5 = 8 → 8×10, or the closest valid integer rect; must not throw or return zero-area
        var rect = CropMath.DefaultCropRect(10, 10);

        Assert.True(rect.Width > 0, "Width must be positive");
        Assert.True(rect.Height > 0, "Height must be positive");
        Assert.True(rect.Left >= 0, "X must be within image bounds");
        Assert.True(rect.Top >= 0, "Y must be within image bounds");
        Assert.True(rect.Left + rect.Width <= 10, "Rect must not exceed image width");
        Assert.True(rect.Top + rect.Height <= 10, "Rect must not exceed image height");
    }

    [Fact]
    public void DefaultCropRect_WidePanorama3000x500_IsHeightLimited400x500()
    {
        // Height-limited: width = 500 * 4/5 = 400
        var rect = CropMath.DefaultCropRect(3000, 500);

        Assert.Equal(400, rect.Width);
        Assert.Equal(500, rect.Height);
    }

    [Fact]
    public void DefaultCropRect_WidePanorama3000x500_IsCenteredHorizontally()
    {
        var rect = CropMath.DefaultCropRect(3000, 500);

        int expectedX = (3000 - 400) / 2; // 1300
        Assert.InRange(rect.Left, expectedX - 1, expectedX + 1);
        Assert.Equal(0, rect.Top);
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1080, 1350)]
    [InlineData(1000, 1000)]
    [InlineData(800, 1000)]
    [InlineData(3000, 500)]
    [InlineData(100, 200)]
    public void DefaultCropRect_Always_IsCenteredWithinOnePx(int imageW, int imageH)
    {
        var rect = CropMath.DefaultCropRect(imageW, imageH);

        // Horizontal center check
        double centerX = rect.Left + rect.Width / 2.0;
        Assert.InRange(centerX, imageW / 2.0 - 1, imageW / 2.0 + 1);

        // Vertical center check
        double centerY = rect.Top + rect.Height / 2.0;
        Assert.InRange(centerY, imageH / 2.0 - 1, imageH / 2.0 + 1);
    }

    // ── ComputeCropRect ──────────────────────────────────────────────────────
    // Signature: ComputeCropRect(int imageWidth, int imageHeight,
    //                            Size cropWindowPixelSize, Vector panOffset, double zoom)
    // panOffset convention: positive X pans the image right on screen (rect.Left decreases);
    //   imgX = -panOffset.X / zoom, imgW = cropWindowPixelSize.Width / zoom

    [Fact]
    public void ComputeCropRect_NoPanZoom1_CropWindowMatchesImage_ReturnsFullImageRect()
    {
        // 1:1 mapping with crop window == image → full-image-sized rect at origin
        var rect = CropMath.ComputeCropRect(
            imageWidth: 800, imageHeight: 1000,
            cropWindowPixelSize: new Size(800, 1000),
            panOffset: new Vector(0, 0),
            zoom: 1.0);

        Assert.Equal(0, rect.Left);
        Assert.Equal(0, rect.Top);
        Assert.Equal(800, rect.Width);
        Assert.Equal(1000, rect.Height);
    }

    [Fact]
    public void ComputeCropRect_NoPanZoom2_480x600Window_1920x1080Image_Returns240x300AtOrigin()
    {
        // zoom=2 → visible image region is halved: 480/2 × 600/2 = 240×300
        // No pan → imgX = 0, imgY = 0 → rect at (0, 0, 240, 300)
        var rect = CropMath.ComputeCropRect(
            imageWidth: 1920, imageHeight: 1080,
            cropWindowPixelSize: new Size(480, 600),
            panOffset: new Vector(0, 0),
            zoom: 2.0);

        Assert.Equal(240, rect.Width);
        Assert.Equal(300, rect.Height);
        Assert.Equal(0, rect.Left);
        Assert.Equal(0, rect.Top);
    }

    [Fact]
    public void ComputeCropRect_NegativePanX_ShiftsRectRightInImageSpace()
    {
        // panOffset.X = -100 means image panned left on screen → crop window sees further right
        // imgX = -(-100) / 1.0 = 100
        var rect = CropMath.ComputeCropRect(
            imageWidth: 800, imageHeight: 1000,
            cropWindowPixelSize: new Size(480, 600),
            panOffset: new Vector(-100, 0),
            zoom: 1.0);

        Assert.Equal(100, rect.Left);
        Assert.Equal(0, rect.Top);
    }

    [Fact]
    public void ComputeCropRect_PanOffset_ShiftsRectByExpectedDelta()
    {
        var noPan = CropMath.ComputeCropRect(
            imageWidth: 800, imageHeight: 1000,
            cropWindowPixelSize: new Size(480, 600),
            panOffset: new Vector(0, 0),
            zoom: 1.0);

        var withPan = CropMath.ComputeCropRect(
            imageWidth: 800, imageHeight: 1000,
            cropWindowPixelSize: new Size(480, 600),
            panOffset: new Vector(-50, 0),
            zoom: 1.0);

        // Moving panOffset by -50 in X → imgX shifts +50 → rect.Left shifts +50
        Assert.Equal(noPan.Left + 50, withPan.Left);
        Assert.Equal(noPan.Top, withPan.Top);
    }

    [Fact]
    public void ComputeCropRect_LargePan_ClampedToImageBounds()
    {
        // Extreme negative pan → imgX would be huge; should clamp to image width
        var rect = CropMath.ComputeCropRect(
            imageWidth: 800, imageHeight: 1000,
            cropWindowPixelSize: new Size(480, 600),
            panOffset: new Vector(-99999, -99999),
            zoom: 1.0);

        Assert.True(rect.Left >= 0, "X must be >= 0 after clamping");
        Assert.True(rect.Top >= 0, "Y must be >= 0 after clamping");
        Assert.True(rect.Left + rect.Width <= 800, "Right edge must not exceed image width");
        Assert.True(rect.Top + rect.Height <= 1000, "Bottom edge must not exceed image height");
    }

    [Theory]
    [InlineData(1920, 1080, 480, 600, 1.0, 0, 0)]
    [InlineData(1920, 1080, 480, 600, 2.0, 0, 0)]
    [InlineData(800, 1000, 480, 600, 1.5, -100, 50)]
    [InlineData(400, 500, 400, 500, 1.0, 0, 0)]
    public void ComputeCropRect_Always_HasPositiveWidthAndHeight(
        int iw, int ih, int ww, int wh, double zoom, double panX, double panY)
    {
        var rect = CropMath.ComputeCropRect(iw, ih,
            new Size(ww, wh), new Vector(panX, panY), zoom);

        Assert.True(rect.Width > 0, "Width must always be positive");
        Assert.True(rect.Height > 0, "Height must always be positive");
    }
}
