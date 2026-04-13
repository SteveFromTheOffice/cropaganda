# Project Context

- **Owner:** Michael Scott
- **Project:** cropaganda — Windows desktop app for fast batch photo cropping to 4:5 (Instagram) format
- **Stack:** C# / WPF, Windows
- **Created:** 2026-04-13

## Learnings
- Image library chosen: WPF built-in (System.Windows.Media.Imaging), no extra NuGet packages. Project structure: src/Cropaganda (WPF app), src/Cropaganda.Tests (xUnit). ICropService interface at src/Cropaganda/Services/ICropService.cs is the abstraction layer.
- CropMath.cs is in Cropaganda.Services namespace (static class). ComputeCropRect takes (int imageWidth, int imageHeight, Size cropWindowPixelSize, Vector panOffset, double zoom) — NOT separate ints for window size. panOffset convention: negative X pans image left on screen, so rect.X = -panOffset.X / zoom. No-pan start position is image origin (0,0), NOT centered — the caller must set panOffset to center the initial view.
- Test project requires <UseWPF>true</UseWPF> in the .csproj to use BitmapSource, Int32Rect, Size, Vector etc.
- CropMathTests.cs covers DefaultCropRect (7 facts + 1 theory) and ComputeCropRect (5 facts + 1 theory). CropServiceTests.cs covers Crop (4 facts), Save+Reload (1 fact), and 3 edge cases (2 skipped, 1 active).
- DefaultCropRect centering: x and y are always within 1px of image center — verified for all standard sizes.
- ComputeCropRect clamping: verified that extreme negative pan is clamped and never produces rect outside image bounds or zero-area rect.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-13: CropMathTests + CropServiceTests written
- 29 passing, 0 failing, 2 skipped
- Validates CropService (Livingston) and UI integration (Linus)

