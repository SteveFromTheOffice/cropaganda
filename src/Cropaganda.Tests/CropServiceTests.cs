using System;
using System.IO;
using SkiaSharp;
using Xunit;
using Cropaganda.Services;

namespace Cropaganda.Tests;

/// <summary>
/// Integration tests for CropService — uses programmatically created SKBitmaps (no external files).
/// Assumes a concrete class CropService : ICropService in Cropaganda.Services.
/// </summary>
public class CropServiceTests
{
    private static SKBitmap CreateTestBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)(x % 256),
                    (byte)(y % 256),
                    (byte)((x + y) % 256),
                    255));
        return bitmap;
    }

    private static ICropService CreateService() => new CropService();

    // ── Crop ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Crop_NormalRect_ReturnsCorrectDimensions()
    {
        var source = CreateTestBitmap(200, 200);
        var svc = CreateService();

        var result = svc.Crop(source, SKRectI.Create(50, 50, 100, 100));

        Assert.Equal(100, result.Width);
        Assert.Equal(100, result.Height);
    }

    [Fact]
    public void Crop_RectEqualToImageBounds_ReturnsSameDimensions()
    {
        var source = CreateTestBitmap(320, 400);
        var svc = CreateService();

        var result = svc.Crop(source, SKRectI.Create(0, 0, 320, 400));

        Assert.Equal(320, result.Width);
        Assert.Equal(400, result.Height);
    }

    [Fact]
    public void Crop_RectBeyondImageBounds_ClampsAndDoesNotThrow()
    {
        // Rect extends 50px beyond the image in both dimensions — should be clamped, not crash.
        var source = CreateTestBitmap(200, 200);
        var svc = CreateService();

        var result = svc.Crop(source, SKRectI.Create(100, 100, 150, 150));

        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        Assert.True(result.Width <= 200);
        Assert.True(result.Height <= 200);
    }

    [Fact]
    public void Crop_ZeroAreaRect_ThrowsArgumentException()
    {
        var source = CreateTestBitmap(200, 200);
        var svc = CreateService();

        Assert.Throws<ArgumentException>(() =>
            svc.Crop(source, SKRectI.Create(50, 50, 0, 0)));
    }

    // ── Save + Reload ─────────────────────────────────────────────────────────

    [Fact]
    public void SaveAndReload_DimensionsMatch()
    {
        var source = CreateTestBitmap(160, 200);
        var svc = CreateService();

        var outputPath = Path.Combine(
            Path.GetTempPath(),
            $"cropaganda_test_{Guid.NewGuid():N}.jpg");

        try
        {
            svc.Save(source, outputPath, jpegQuality: 95);
            var reloaded = SKBitmap.Decode(outputPath);

            Assert.Equal(source.Width, reloaded.Width);
            Assert.Equal(source.Height, reloaded.Height);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    // ── LoadImage edge cases ─────────────────────────────────────────────────

    [Fact(Skip = "Requires a real non-image file on disk — manual / exploratory test")]
    public void LoadImage_NonImageFile_SurfacesError()
    {
        // Drop a .txt file in — should throw, not silently succeed.
        var svc = CreateService();
        _ = svc.LoadImage("not_an_image.txt");
    }

    [Fact]
    public void LoadImage_ZeroByteFile_ThrowsInvalidOperationException()
    {
        var svc = CreateService();
        var emptyPath = Path.Combine(
            Path.GetTempPath(),
            $"cropaganda_empty_{Guid.NewGuid():N}.jpg");

        File.WriteAllBytes(emptyPath, Array.Empty<byte>());
        try
        {
            Assert.Throws<InvalidOperationException>(() => svc.LoadImage(emptyPath));
        }
        finally
        {
            if (File.Exists(emptyPath))
                File.Delete(emptyPath);
        }
    }

    [Fact(Skip = "Performance test — skipped by default. Run manually with a 48MP+ RAW or JPEG.")]
    public void LoadImage_VeryLargeImage_PerformanceTest()
    {
        // Flag: load a ~8000×6000 image and verify it completes in reasonable time.
        var svc = CreateService();
        _ = svc.LoadImage(@"C:\TestImages\48mp_test.jpg");
    }
}
