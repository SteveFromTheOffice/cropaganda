using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Cropaganda;

/// <summary>
/// Custom Avalonia Control that renders the image + 4:5 crop overlay.
/// The crop box is FIXED on screen; the image pans/zooms behind it.
/// </summary>
public class CropCanvas : Control
{
    private const double CropAspectRatio = 4.0 / 5.0;
    private const double CropBoxHeightFraction = 0.78;
    private const double MaxZoom = 10.0;

    private static readonly SolidColorBrush BackgroundBrush = new(Color.FromRgb(0x1E, 0x1E, 0x1E));
    private static readonly SolidColorBrush DimBrush = new(Color.FromArgb(0xB0, 0x00, 0x00, 0x00));
    private static readonly IPen CropBorderPen = new Pen(new SolidColorBrush(Colors.White), 2.0);
    private static readonly IPen ThirdsPen = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), 0.75);

    private SKBitmap? _skImage;
    private Bitmap? _avaloniaImage;
    private double _zoom = 1.0;
    private Vector _imageOffset;
    private bool _needsViewReset;

    private Point _dragStart;
    private Vector _dragStartOffset;
    private bool _isDragging;

    public SKBitmap? Image
    {
        get => _skImage;
        set
        {
            _avaloniaImage?.Dispose();
            _skImage = value;
            _avaloniaImage = value != null ? ConvertToAvaloniaBitmap(value) : null;
            _needsViewReset = true;
            if (Bounds.Width > 0 && Bounds.Height > 0)
            {
                ResetView();
                _needsViewReset = false;
            }
            InvalidateVisual();
        }
    }

    private static Bitmap ConvertToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }

    /// <summary>Returns the crop rectangle in image native pixel coordinates.</summary>
    public SKRectI GetCropRect()
    {
        if (_skImage == null || _avaloniaImage == null) return SKRectI.Empty;

        var cropBox = GetCropBoxRect();
        var imageRect = GetImageDisplayRect();

        if (imageRect.Width <= 0 || imageRect.Height <= 0) return SKRectI.Empty;

        double x = (cropBox.X - imageRect.X) * _skImage.Width / imageRect.Width;
        double y = (cropBox.Y - imageRect.Y) * _skImage.Height / imageRect.Height;
        double w = cropBox.Width  * _skImage.Width  / imageRect.Width;
        double h = cropBox.Height * _skImage.Height / imageRect.Height;

        double x2 = Math.Min(x + w, _skImage.Width);
        double y2 = Math.Min(y + h, _skImage.Height);
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        w = x2 - x;
        h = y2 - y;

        if (w <= 0 || h <= 0) return SKRectI.Empty;

        return SKRectI.Create(
            (int)Math.Round(x), (int)Math.Round(y),
            (int)Math.Round(w), (int)Math.Round(h));
    }

    private Rect GetCropBoxRect()
    {
        double canvasW = Bounds.Width;
        double canvasH = Bounds.Height;

        double boxH = canvasH * CropBoxHeightFraction;
        double boxW = boxH * CropAspectRatio;

        if (boxW > canvasW * 0.88)
        {
            boxW = canvasW * 0.88;
            boxH = boxW / CropAspectRatio;
        }

        return new Rect((canvasW - boxW) / 2, (canvasH - boxH) / 2, boxW, boxH);
    }

    private Rect GetImageDisplayRect()
    {
        if (_avaloniaImage == null) return default;

        double dispW = _avaloniaImage.Size.Width  * _zoom;
        double dispH = _avaloniaImage.Size.Height * _zoom;

        double cx = Bounds.Width  / 2 + _imageOffset.X;
        double cy = Bounds.Height / 2 + _imageOffset.Y;

        return new Rect(cx - dispW / 2, cy - dispH / 2, dispW, dispH);
    }

    public void ResetView()
    {
        if (_avaloniaImage == null) return;
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var cropBox = GetCropBoxRect();
        double zoomW = cropBox.Width  / _avaloniaImage.Size.Width;
        double zoomH = cropBox.Height / _avaloniaImage.Size.Height;
        _zoom = Math.Max(zoomW, zoomH);
        _zoom = Math.Max(_zoom, GetMinZoom());
        _imageOffset = new Vector(0, 0);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_needsViewReset && _avaloniaImage != null)
        {
            ResetView();
            _needsViewReset = false;
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.DrawRectangle(BackgroundBrush, null, bounds);

        if (_avaloniaImage == null)
        {
            DrawDropHint(context);
            return;
        }

        var cropBox   = GetCropBoxRect();
        var imageRect = GetImageDisplayRect();

        context.DrawImage(_avaloniaImage, imageRect);

        if (cropBox.Top > 0)
            context.DrawRectangle(DimBrush, null, new Rect(0, 0, Bounds.Width, cropBox.Top));
        if (cropBox.Bottom < Bounds.Height)
            context.DrawRectangle(DimBrush, null, new Rect(0, cropBox.Bottom, Bounds.Width, Bounds.Height - cropBox.Bottom));
        if (cropBox.Left > 0)
            context.DrawRectangle(DimBrush, null, new Rect(0, cropBox.Top, cropBox.Left, cropBox.Height));
        if (cropBox.Right < Bounds.Width)
            context.DrawRectangle(DimBrush, null, new Rect(cropBox.Right, cropBox.Top, Bounds.Width - cropBox.Right, cropBox.Height));

        context.DrawRectangle(null, CropBorderPen, cropBox);

        double tw = cropBox.Width  / 3;
        double th = cropBox.Height / 3;
        context.DrawLine(ThirdsPen, new Point(cropBox.Left + tw,     cropBox.Top),    new Point(cropBox.Left + tw,     cropBox.Bottom));
        context.DrawLine(ThirdsPen, new Point(cropBox.Left + tw * 2, cropBox.Top),    new Point(cropBox.Left + tw * 2, cropBox.Bottom));
        context.DrawLine(ThirdsPen, new Point(cropBox.Left,          cropBox.Top + th),  new Point(cropBox.Right, cropBox.Top + th));
        context.DrawLine(ThirdsPen, new Point(cropBox.Left,          cropBox.Top + th * 2), new Point(cropBox.Right, cropBox.Top + th * 2));
    }

    private void DrawDropHint(DrawingContext context)
    {
        var brush      = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF));
        var smallBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));

        var largeText = new FormattedText(
            "Drop photos here",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Arial"),
            26, brush);

        var smallText = new FormattedText(
            "JPG · PNG · BMP · TIFF · WebP",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Arial"),
            14, smallBrush);

        double cx = Bounds.Width  / 2;
        double cy = Bounds.Height / 2;
        context.DrawText(largeText, new Point(cx - largeText.Width / 2, cy - largeText.Height / 2 - 14));
        context.DrawText(smallText, new Point(cx - smallText.Width / 2, cy + smallText.Height / 2 + 4));
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_avaloniaImage == null) return;

        var mousePos = e.GetPosition(this);
        double factor  = e.Delta.Y > 0 ? 1.12 : 1.0 / 1.12;
        double newZoom = Math.Clamp(_zoom * factor, GetMinZoom(), MaxZoom);
        if (Math.Abs(newZoom - _zoom) < 1e-6) return;

        var canvasCenter = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var rel          = mousePos - canvasCenter - _imageOffset;
        var imagePoint   = new Vector(rel.X / _zoom, rel.Y / _zoom);

        _zoom = newZoom;
        _imageOffset = new Vector(
            mousePos.X - canvasCenter.X - imagePoint.X * _zoom,
            mousePos.Y - canvasCenter.Y - imagePoint.Y * _zoom);

        ClampPan();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_avaloniaImage == null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _dragStart       = e.GetPosition(this);
        _dragStartOffset = _imageOffset;
        _isDragging      = true;
        e.Pointer.Capture(this);
        Cursor    = new Cursor(StandardCursorType.SizeAll);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging || _avaloniaImage == null) return;

        var pos      = e.GetPosition(this);
        _imageOffset = _dragStartOffset + (pos - _dragStart);
        ClampPan();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isDragging) return;
        _isDragging = false;
        e.Pointer.Capture(null);
        Cursor = Cursor.Default;
    }

    private double GetMinZoom()
    {
        if (_avaloniaImage == null ||
            _avaloniaImage.Size.Width  <= 0 ||
            _avaloniaImage.Size.Height <= 0) return 0.05;

        var cropBox = GetCropBoxRect();
        if (cropBox.Width <= 0 || cropBox.Height <= 0) return 0.05;

        double zoomW = cropBox.Width  / _avaloniaImage.Size.Width;
        double zoomH = cropBox.Height / _avaloniaImage.Size.Height;
        return Math.Max(0.05, Math.Max(zoomW, zoomH));
    }

    private void ClampPan()
    {
        if (_avaloniaImage == null) return;
        var cropBox = GetCropBoxRect();
        if (cropBox.Width <= 0 || cropBox.Height <= 0) return;

        double dispW = _avaloniaImage.Size.Width  * _zoom;
        double dispH = _avaloniaImage.Size.Height * _zoom;
        double cx    = Bounds.Width  / 2;
        double cy    = Bounds.Height / 2;

        double minOX = cropBox.Right  - cx - dispW / 2;
        double maxOX = cropBox.Left   - cx + dispW / 2;
        double minOY = cropBox.Bottom - cy - dispH / 2;
        double maxOY = cropBox.Top    - cy + dispH / 2;

        if (minOX > maxOX) { double mid = (minOX + maxOX) / 2; minOX = maxOX = mid; }
        if (minOY > maxOY) { double mid = (minOY + maxOY) / 2; minOY = maxOY = mid; }

        _imageOffset = new Vector(
            Math.Clamp(_imageOffset.X, minOX, maxOX),
            Math.Clamp(_imageOffset.Y, minOY, maxOY));
    }
}
