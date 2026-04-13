using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cropaganda.Services;

namespace Cropaganda.Tests;

/// <summary>
/// Integration tests for CropService — uses programmatically created BitmapSources (no external files).
/// Assumes a concrete class CropService : ICropService in Cropaganda.Services.
/// </summary>
public class CropServiceTests
{
    private static BitmapSource CreateTestBitmap(int width, int height)
    {
        var pixels = new byte[width * height * 4]; // BGRA
        // Simple gradient so pixels are non-trivially patterned
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                pixels[i + 0] = (byte)(x % 256);       // B
                pixels[i + 1] = (byte)(y % 256);       // G
                pixels[i + 2] = (byte)((x + y) % 256); // R
                pixels[i + 3] = 255;                   // A
            }
        return BitmapSource.Create(width, height, 96, 96,
            PixelFormats.Bgra32, null, pixels, width * 4);
    }

    private static ICropService CreateService() => new CropService();

    // ── Crop ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Crop_NormalRect_ReturnsCorrectDimensions()
    {
        var source = CreateTestBitmap(200, 200);
        var svc = CreateService();

        var result = svc.Crop(source, new Int32Rect(50, 50, 100, 100));

        Assert.Equal(100, result.PixelWidth);
        Assert.Equal(100, result.PixelHeight);
    }

    [Fact]
    public void Crop_RectEqualToImageBounds_ReturnsSameDimensions()
    {
        var source = CreateTestBitmap(320, 400);
        var svc = CreateService();

        var result = svc.Crop(source, new Int32Rect(0, 0, 320, 400));

        Assert.Equal(320, result.PixelWidth);
        Assert.Equal(400, result.PixelHeight);
    }

    [Fact]
    public void Crop_RectBeyondImageBounds_ClampsAndDoesNotThrow()
    {
        // Rect extends 50px beyond the image in both dimensions — should be clamped, not crash.
        var source = CreateTestBitmap(200, 200);
        var svc = CreateService();

        var result = svc.Crop(source, new Int32Rect(100, 100, 150, 150));

        Assert.True(result.PixelWidth > 0);
        Assert.True(result.PixelHeight > 0);
        Assert.True(result.PixelWidth <= 200);
        Assert.True(result.PixelHeight <= 200);
    }

    [Fact]
    public void Crop_ZeroAreaRect_ThrowsArgumentException()
    {
        var source = CreateTestBitmap(200, 200);
        var svc = CreateService();

        Assert.Throws<ArgumentException>(() =>
            svc.Crop(source, new Int32Rect(50, 50, 0, 0)));
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
            var reloaded = svc.LoadImage(outputPath);

            Assert.Equal(source.PixelWidth, reloaded.PixelWidth);
            Assert.Equal(source.PixelHeight, reloaded.PixelHeight);
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
            // WIC may hold a brief read lock; retry delete a few times
            for (int i = 0; i < 5; i++)
            {
                try { File.Delete(emptyPath); break; }
                catch (IOException) { System.Threading.Thread.Sleep(50); }
            }
        }
    }

    [Fact(Skip = "Performance test — skipped by default. Run manually with a 48MP+ RAW or JPEG.")]
    public void LoadImage_VeryLargeImage_PerformanceTest()
    {
        // Flag: load a ~8000×6000 image and verify it completes in reasonable time.
        // Route to Livingston if perf is unacceptable.
        var svc = CreateService();
        _ = svc.LoadImage(@"C:\TestImages\48mp_test.jpg");
    }
}
