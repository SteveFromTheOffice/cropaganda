# Project Context

- **Owner:** Michael Scott
- **Project:** cropaganda — Windows desktop app for fast batch photo cropping to 4:5 (Instagram) format
- **Stack:** C# / WPF, Windows
- **Created:** 2026-04-13

## Learnings
- Image library chosen: WPF built-in (System.Windows.Media.Imaging), no extra NuGet packages. Project structure: src/Cropaganda (WPF app), src/Cropaganda.Tests (xUnit). ICropService interface at src/Cropaganda/Services/ICropService.cs is the abstraction layer.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-13: CropService + CropMath implemented
- CropService.cs (LoadImage/Crop/Save via WIC pipeline), CropMath.cs (DefaultCropRect + ComputeCropRect)
- Integrates with UI (Linus)
- Build clean, all tests pass


### 2026-04-13: CropService + CropMath implemented
- `CropService` implements `ICropService` using WIC/WPF pipeline. `LoadImage` uses `BitmapDecoder.Create` with `BitmapCacheOption.OnLoad` so the file handle is released immediately. Returns a frozen `BitmapFrame`.
- `Crop` uses `CroppedBitmap`, clamps the rect to image bounds before calling, throws `ArgumentException` if clamped area is zero.
- `Save` always writes JPEG via `JpegBitmapEncoder`. Quality is clamped to 1–100. Metadata is cloned when provided and attached via `BitmapFrame.Create(image, null, metadataClone, null)`.
- `CropMath.DefaultCropRect`: `cropWidth = min(imageWidth, imageHeight * 4 / 5)` then `cropHeight = cropWidth * 5 / 4`. Integer division can round down height by 1px — an extra `Math.Min(cropHeight, imageHeight)` guard prevents any bound overrun.
- `CropMath.ComputeCropRect`: converts screen-space pan/zoom to image-pixel coords via `imgX = -panOffset.X / zoom`. All values clamped to image bounds, minimum 1px enforced.
- **Math gotcha:** `imageHeight * 4 / 5` in C# integer arithmetic truncates. This is intentional — we want the largest *whole-pixel* 4:5 rect. The subsequent `cropHeight = cropWidth * 5 / 4` may then slightly undercount; the `Math.Min` clamp is the safety net.
- No DI container used — `CropService` has a parameterless constructor; Linus instantiates directly.
