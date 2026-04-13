# Project Context

- **Owner:** Michael Scott
- **Project:** cropaganda — Windows desktop app for fast batch photo cropping to 4:5 (Instagram) format
- **Stack:** C# / WPF, Windows
- **Created:** 2026-04-13

## Learnings
- Image library chosen: WPF built-in (System.Windows.Media.Imaging), no extra NuGet packages. Project structure: src/Cropaganda (WPF app), src/Cropaganda.Tests (xUnit). ICropService interface at src/Cropaganda/Services/ICropService.cs is the abstraction layer.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-13: Full crop UI built
- CropCanvas.cs (custom FrameworkElement, zoom/pan/overlay), MainWindow.xaml (dark theme, overlays), MainWindow.xaml.cs (drag-drop, async load, keyboard handling)
- Integrates with CropService (Livingston)
- Build clean, all tests pass


### 2026-04-13: Full crop UI built
- **CropCanvas.cs** (`FrameworkElement` subclass): renders image + 4:5 overlay entirely in `OnRender`. Fixed crop box in screen space; image zooms/pans behind it. All coordinates in WPF DIPs; only `PixelWidth/PixelHeight` used when computing the final `Int32Rect` crop.
- **Zoom math**: cursor-relative zoom computed by deriving the image-space point under the cursor before and after zoom, then adjusting `_imageOffset` to keep that point fixed.
- **Pan clamping**: `ClampPan()` enforces that the image always covers the crop box. Computes min/max offsets from crop box edges and image display size; collapses to center if image is smaller than crop box on an axis.
- **View reset**: uses a `_needsViewReset` flag so `OnRenderSizeChanged` only resets zoom/pan when a new image is just loaded (not on every window resize).
- **DPI correctness**: uses `_image.Width` (DIPs) for layout math, and `_image.PixelWidth` only in `GetCropRect()` to convert back to pixel coordinates. This handles images with non-96 DPI metadata correctly.
- **MainWindow.xaml.cs**: pure code-behind (no MVVM needed for this use case). `async void` used for `LoadCurrentImageAsync` and `SaveAndAdvance` — only acceptable because they're UI event handlers and errors are caught and shown as toasts.
- **Error handling**: corrupt/missing images show a 5-second auto-dismissing error toast; the image is skipped from the list. No crashes.
- **Smart-quote gotcha**: when writing string interpolation with `"` inside `$"..."`, use `\"` — the editor sometimes inserts Unicode curly quotes which are invalid C# string content and cause parse errors.
- **CropService** was already implemented by Livingston — don't overwrite it.

