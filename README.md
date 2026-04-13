# Cropaganda

Fast batch photo cropping to 4:5 (Instagram) format. Drag photos in, zoom/pan to frame the crop, hit Enter. Output images are written to an output folder as you go.

## Stack

- **C# / WPF** on .NET 8 (Windows)
- **Image processing:** WPF built-in imaging (`System.Windows.Media.Imaging` / Windows Imaging Component)
  - Zero external dependencies for image ops
  - JPEG quality control via `JpegBitmapEncoder.QualityLevel`
  - EXIF metadata preservation via `BitmapMetadata`
- **Tests:** xUnit

### Why WPF built-in imaging?

Cropaganda does one thing: crop images. For a pure crop operation (extracting a pixel rectangle), all imaging libraries produce identical output — no resampling is involved. WPF's `BitmapSource` is already the native image type in our UI framework, so we avoid conversion overhead and keep the dependency count at zero. JPEG encoding quality at 95+ is indistinguishable across libraries. If we ever need advanced processing (filters, batch resize), we can add a library then. YAGNI.

## Build

```bash
dotnet build Cropaganda.sln
```

## Test

```bash
dotnet test Cropaganda.sln
```

## Project Structure

```
Cropaganda.sln
src/
  Cropaganda/              # WPF app
    Services/
      ICropService.cs      # Core crop interface
    App.xaml
    MainWindow.xaml
  Cropaganda.Tests/        # xUnit tests
```
