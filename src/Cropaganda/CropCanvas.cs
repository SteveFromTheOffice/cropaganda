using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cropaganda;

/// <summary>
/// Custom FrameworkElement that renders the image + 4:5 crop overlay.
/// The crop box is FIXED on screen; the image pans/zooms behind it.
/// All coordinates are WPF device-independent units (DIPs) unless noted.
/// </summary>
public class CropCanvas : FrameworkElement
{
    // 4:5 portrait ratio: width/height = 0.8
    private const double CropAspectRatio = 4.0 / 5.0;
    private const double CropBoxHeightFraction = 0.78;
    private const double MaxZoom = 10.0;

    private static readonly SolidColorBrush BackgroundBrush =
        new(Color.FromRgb(0x1E, 0x1E, 0x1E));
    private static readonly SolidColorBrush DimBrush =
        new(Color.FromArgb(0xB0, 0x00, 0x00, 0x00));
    private static readonly Pen CropBorderPen =
        new(new SolidColorBrush(Colors.White), 2.0);
    private static readonly Pen ThirdsPen =
        new(new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), 0.75);

    static CropCanvas()
    {
        BackgroundBrush.Freeze();
        DimBrush.Freeze();
        ((SolidColorBrush)CropBorderPen.Brush).Freeze();
        CropBorderPen.Freeze();
        ((SolidColorBrush)ThirdsPen.Brush).Freeze();
        ThirdsPen.Freeze();
    }

    private BitmapSource? _image;
    private double _zoom = 1.0;
    private Vector _imageOffset;   // offset of image center from canvas center, in DIPs
    private bool _needsViewReset;

    // Drag state
    private Point _dragStart;
    private Vector _dragStartOffset;
    private bool _isDragging;

    public BitmapSource? Image
    {
        get => _image;
        set
        {
            _image = value;
            _needsViewReset = true;
            if (ActualWidth > 0 && ActualHeight > 0)
            {
                ResetView();
                _needsViewReset = false;
            }
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Returns the crop rectangle in the image's native pixel coordinates.
    /// Returns Int32Rect.Empty if no image is loaded.
    /// </summary>
    public Int32Rect GetCropRect()
    {
        if (_image == null) return Int32Rect.Empty;

        var cropBox = GetCropBoxRect();
        var imageRect = GetImageDisplayRect();

        if (imageRect.Width <= 0 || imageRect.Height <= 0) return Int32Rect.Empty;

        // Convert crop box from DIPs to image pixel coordinates
        double x = (cropBox.X - imageRect.X) * _image.PixelWidth / imageRect.Width;
        double y = (cropBox.Y - imageRect.Y) * _image.PixelHeight / imageRect.Height;
        double w = cropBox.Width * _image.PixelWidth / imageRect.Width;
        double h = cropBox.Height * _image.PixelHeight / imageRect.Height;

        // Clamp to image bounds
        double x2 = Math.Min(x + w, _image.PixelWidth);
        double y2 = Math.Min(y + h, _image.PixelHeight);
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        w = x2 - x;
        h = y2 - y;

        if (w <= 0 || h <= 0) return Int32Rect.Empty;

        return new Int32Rect((int)Math.Round(x), (int)Math.Round(y),
                             (int)Math.Round(w), (int)Math.Round(h));
    }

    // Returns the crop box rect in DIPs, centered in the canvas.
    private Rect GetCropBoxRect()
    {
        double canvasW = ActualWidth;
        double canvasH = ActualHeight;

        double boxH = canvasH * CropBoxHeightFraction;
        double boxW = boxH * CropAspectRatio;

        // Don't overflow horizontally
        if (boxW > canvasW * 0.88)
        {
            boxW = canvasW * 0.88;
            boxH = boxW / CropAspectRatio;
        }

        return new Rect((canvasW - boxW) / 2, (canvasH - boxH) / 2, boxW, boxH);
    }

    // Returns the display rect of the image in DIPs.
    // Image center is at (canvasCenter + _imageOffset).
    private Rect GetImageDisplayRect()
    {
        if (_image == null) return Rect.Empty;

        // Use _image.Width/.Height (in DIPs, honoring image DPI metadata) for layout
        double dispW = _image.Width * _zoom;
        double dispH = _image.Height * _zoom;

        double cx = ActualWidth / 2 + _imageOffset.X;
        double cy = ActualHeight / 2 + _imageOffset.Y;

        return new Rect(cx - dispW / 2, cy - dispH / 2, dispW, dispH);
    }

    public void ResetView()
    {
        if (_image == null) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        var cropBox = GetCropBoxRect();

        // Scale image to just cover the crop box (cover, not contain)
        double zoomW = cropBox.Width / _image.Width;
        double zoomH = cropBox.Height / _image.Height;
        _zoom = Math.Max(zoomW, zoomH);
        _zoom = Math.Max(_zoom, GetMinZoom());

        _imageOffset = new Vector(0, 0);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (_needsViewReset && _image != null)
        {
            ResetView();
            _needsViewReset = false;
        }
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        dc.DrawRectangle(BackgroundBrush, null, bounds);

        if (_image == null)
        {
            DrawDropHint(dc);
            return;
        }

        var cropBox = GetCropBoxRect();
        var imageRect = GetImageDisplayRect();

        dc.DrawImage(_image, imageRect);

        // Dim the 4 areas outside the crop box
        if (cropBox.Top > 0)
            dc.DrawRectangle(DimBrush, null, new Rect(0, 0, ActualWidth, cropBox.Top));
        if (cropBox.Bottom < ActualHeight)
            dc.DrawRectangle(DimBrush, null, new Rect(0, cropBox.Bottom, ActualWidth, ActualHeight - cropBox.Bottom));
        if (cropBox.Left > 0)
            dc.DrawRectangle(DimBrush, null, new Rect(0, cropBox.Top, cropBox.Left, cropBox.Height));
        if (cropBox.Right < ActualWidth)
            dc.DrawRectangle(DimBrush, null, new Rect(cropBox.Right, cropBox.Top, ActualWidth - cropBox.Right, cropBox.Height));

        // Crop border
        dc.DrawRectangle(null, CropBorderPen, cropBox);

        // Rule-of-thirds guides
        double tw = cropBox.Width / 3;
        double th = cropBox.Height / 3;
        dc.DrawLine(ThirdsPen, new Point(cropBox.Left + tw, cropBox.Top), new Point(cropBox.Left + tw, cropBox.Bottom));
        dc.DrawLine(ThirdsPen, new Point(cropBox.Left + tw * 2, cropBox.Top), new Point(cropBox.Left + tw * 2, cropBox.Bottom));
        dc.DrawLine(ThirdsPen, new Point(cropBox.Left, cropBox.Top + th), new Point(cropBox.Right, cropBox.Top + th));
        dc.DrawLine(ThirdsPen, new Point(cropBox.Left, cropBox.Top + th * 2), new Point(cropBox.Right, cropBox.Top + th * 2));
    }

    private void DrawDropHint(DrawingContext dc)
    {
        double pxPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var brush = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF));

        var largeText = new FormattedText("Drop photos here",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, 26, brush, pxPerDip);

        var smallText = new FormattedText("JPG · PNG · BMP · TIFF · WebP",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, 14, new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)), pxPerDip);

        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;
        dc.DrawText(largeText, new Point(cx - largeText.Width / 2, cy - largeText.Height / 2 - 14));
        dc.DrawText(smallText, new Point(cx - smallText.Width / 2, cy + smallText.Height / 2 + 4));
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_image == null) return;

        var mousePos = e.GetPosition(this);
        double factor = e.Delta > 0 ? 1.12 : 1.0 / 1.12;
        double newZoom = Math.Clamp(_zoom * factor, GetMinZoom(), MaxZoom);

        if (Math.Abs(newZoom - _zoom) < 1e-6) return;

        // Zoom toward the cursor: the image-space point under the cursor stays fixed.
        // mousePos = canvasCenter + imageOffset + imagePoint * zoom  (in DIP space where image is width*zoom wide)
        // imagePoint (in image DIPs) = (mousePos - canvasCenter - imageOffset) / zoom
        var canvasCenter = new Point(ActualWidth / 2, ActualHeight / 2);
        var rel = mousePos - canvasCenter - _imageOffset;
        var imagePoint = new Vector(rel.X / _zoom, rel.Y / _zoom);

        _zoom = newZoom;
        _imageOffset = new Vector(
            mousePos.X - canvasCenter.X - imagePoint.X * _zoom,
            mousePos.Y - canvasCenter.Y - imagePoint.Y * _zoom);

        ClampPan();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (_image == null) return;

        _dragStart = e.GetPosition(this);
        _dragStartOffset = _imageOffset;
        _isDragging = true;
        CaptureMouse();
        Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging || _image == null) return;

        var pos = e.GetPosition(this);
        _imageOffset = _dragStartOffset + (pos - _dragStart);
        ClampPan();
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_isDragging) return;
        _isDragging = false;
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    private double GetMinZoom()
    {
        if (_image == null || _image.Width <= 0 || _image.Height <= 0) return 0.05;
        var cropBox = GetCropBoxRect();
        if (cropBox.IsEmpty) return 0.05;
        double zoomW = cropBox.Width / _image.Width;
        double zoomH = cropBox.Height / _image.Height;
        return Math.Max(0.05, Math.Max(zoomW, zoomH));
    }

    private void ClampPan()
    {
        if (_image == null) return;
        var cropBox = GetCropBoxRect();
        if (cropBox.IsEmpty) return;

        double dispW = _image.Width * _zoom;
        double dispH = _image.Height * _zoom;

        // Image center is at (canvasCenter + imageOffset).
        // imageLeft  = canvasCenter.X + imageOffset.X - dispW/2
        // Constraint: imageLeft  <= cropBox.Left   → imageOffset.X >= cropBox.Left  - canvasCenter.X + dispW/2
        // Constraint: imageRight >= cropBox.Right  → imageOffset.X <= cropBox.Right - canvasCenter.X - dispW/2
        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;

        double minOX = cropBox.Left - cx + dispW / 2;
        double maxOX = cropBox.Right - cx - dispW / 2;
        double minOY = cropBox.Top - cy + dispH / 2;
        double maxOY = cropBox.Bottom - cy - dispH / 2;

        // If image is smaller than crop box on an axis, center it on that axis
        if (minOX > maxOX) { double mid = (minOX + maxOX) / 2; minOX = maxOX = mid; }
        if (minOY > maxOY) { double mid = (minOY + maxOY) / 2; minOY = maxOY = mid; }

        _imageOffset = new Vector(
            Math.Clamp(_imageOffset.X, minOX, maxOX),
            Math.Clamp(_imageOffset.Y, minOY, maxOY));
    }
}
