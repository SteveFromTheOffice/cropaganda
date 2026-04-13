# Project Context

- **Owner:** Michael Scott
- **Project:** cropaganda — Windows desktop app for fast batch photo cropping to 4:5 (Instagram) format
- **Stack:** C# / WPF, Windows
- **Created:** 2026-04-13

## Learnings
- Image library chosen: WPF built-in (System.Windows.Media.Imaging), no extra NuGet packages. Project structure: src/Cropaganda (WPF app), src/Cropaganda.Tests (xUnit). ICropService interface at src/Cropaganda/Services/ICropService.cs is the abstraction layer.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-14: macOS compatibility — OutputType and COM interop
- **Decision:** Changed `<OutputType>` from `WinExe` to `Exe`. `WinExe` is Windows-only (hides console window); it breaks Avalonia's macOS process lifecycle handling.
- **Decision:** Removed `<BuiltInComInteropSupport>true</BuiltInComInteropSupport>`. This is a Windows COM source-generator flag. It does not compile cleanly on macOS and the app uses no COM APIs.
- The project already had `SkiaSharp.NativeAssets.macOS` and `SkiaSharp.NativeAssets.Win32` so native rendering was already cross-platform — only the project metadata needed fixing.

### 2026-04-13: Image library decision & solution scaffolding
- **Decision:** WPF built-in imaging (`System.Windows.Media.Imaging` / WIC) — zero external deps. Crop operations don't resample pixels so all libraries produce identical output. YAGNI on heavier libs.
- **Escape hatch:** `ICropService` interface abstracts the implementation. Swap to ImageSharp later if needed (resize, filters, cross-platform).
- **Solution structure created:**
  - `Cropaganda.sln`
  - `src/Cropaganda/Cropaganda.csproj` — WPF app, net8.0-windows
  - `src/Cropaganda/Services/ICropService.cs` — LoadImage, Crop, Save
  - `src/Cropaganda/App.xaml`, `MainWindow.xaml` — minimal placeholders
  - `src/Cropaganda.Tests/Cropaganda.Tests.csproj` — xUnit, net8.0-windows, references main project
  - `nuget.config` — pinned to nuget.org (avoids Azure DevOps feed issues on this machine)
  - `README.md` — stack rationale, build/test commands, project structure
