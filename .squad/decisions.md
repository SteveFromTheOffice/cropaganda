# Squad Decisions

<!-- Scribe merges inbox entries here. Append only — never edit existing entries. -->

### Image Processing Library Decision

**Date:** 2026-04-13
**By:** Rusty (Lead)
**Status:** Decided

#### Decision

Use **WPF built-in imaging** (`System.Windows.Media.Imaging` / Windows Imaging Component) — no additional NuGet package.

#### Options Considered

| Library | Verdict |
|---------|---------|
| `System.Drawing.Common` | Deprecated on non-Windows, GDI+ quality quirks. **No.** |
| `SkiaSharp` | Excellent quality, but adds native C++ dependency (Skia). Overkill for crop. **No.** |
| `ImageSharp` (SixLabors) | Pure .NET, great API. License changed to paid for commercial v3+. Adds dependency we don't need yet. **No.** |
| `WPF / WIC built-in` | Already in our stack. Zero deps. Identical output for crop ops (no resampling). JPEG quality control. EXIF preservation. **Yes.** |
| `Magick.NET` | Heavy native dep. Way overkill. **No.** |

#### Rationale

1. **Cropaganda crops images. That's it.** Cropping extracts a pixel rectangle — no resampling, no filters, no compositing. Every library produces identical output for this operation.
2. **`BitmapSource` is WPF's native image type.** Using it means zero conversion overhead between the UI layer and the processing layer.
3. **JPEG quality control** is available via `JpegBitmapEncoder.QualityLevel` (1–100). At quality 95+, codec differences are imperceptible.
4. **EXIF preservation** is supported via `BitmapFrame.Create` with metadata parameter.
5. **Zero additional dependencies** means simpler builds, smaller binaries, no native DLL headaches, no license concerns.

#### Escape Hatch

If we later need resize, filters, or cross-platform support, we add `ImageSharp` at that point. The `ICropService` interface abstracts the implementation, so swapping is a single-class change. But we don't add it until we need it.

### 2026-04-13: Project kickoff
**By:** Michael Scott
**What:** Building cropaganda — a Windows desktop app (C#/WPF) for fast batch photo cropping to 4:5 aspect ratio (Instagram format). Users drag photos in, use mouse wheel to zoom the crop window, drag to pan, and hit Enter to confirm crop and advance to next image. Output images written to an output folder as the user progresses.
**Why:** Project inception — captured for team memory.
