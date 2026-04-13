using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Cropaganda.Services;
using SkiaSharp;

namespace Cropaganda;

public partial class MainWindow : Window
{
    private static readonly string[] SupportedExtensions =
        [".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".webp"];

    private readonly ICropService _cropService = new CropService();
    private readonly List<string> _imagePaths = [];

    private int _currentIndex = -1;
    private SKBitmap? _currentBitmap;
    private string? _outputFolder;
    private int _savedCount;

    private DispatcherTimer? _errorTimer;

    public MainWindow()
    {
        InitializeComponent();

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        KeyDown += OnKeyDown;
        DoneCloseButton.Click += (_, _) => ResetSession();
        Opened += (_, _) => Focus();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var storageItems = e.Data.GetFiles();
        if (storageItems == null) return;

        var imageFiles = storageItems
            .Select(item => item.Path.LocalPath)
            .Where(IsImageFile)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (imageFiles.Count == 0) return;

        bool wasEmpty = _imagePaths.Count == 0;
        _imagePaths.AddRange(imageFiles);

        if (wasEmpty)
        {
            _savedCount = 0;
            _outputFolder = null;
            _currentIndex = 0;
            LoadCurrentImageAsync();
        }

        UpdateStatus();
    }

    private static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path);
        return SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private async void LoadCurrentImageAsync()
    {
        if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count) return;

        var path = _imagePaths[_currentIndex];
        UpdateStatus();
        CropCanvas.Image = null;

        try
        {
            var bitmap = await Task.Run(() => _cropService.LoadImage(path));
            _currentBitmap?.Dispose();
            _currentBitmap = bitmap;
            CropCanvas.Image = bitmap;
            _outputFolder ??= Path.Combine(Path.GetDirectoryName(path)!, "cropped");
        }
        catch (Exception ex)
        {
            ShowError($"Could not load \"{Path.GetFileName(path)}\": {ex.Message}");
            SkipCurrentImage();
        }
    }

    private void SkipCurrentImage()
    {
        if (_imagePaths.Count == 0) return;
        _imagePaths.RemoveAt(_currentIndex);

        if (_imagePaths.Count == 0)
        {
            _currentIndex = -1;
            CropCanvas.Image = null;
            UpdateStatus();
            return;
        }

        if (_currentIndex >= _imagePaths.Count)
            _currentIndex = _imagePaths.Count - 1;

        LoadCurrentImageAsync();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                SaveAndAdvance();
                e.Handled = true;
                break;
            case Key.Right:
                Navigate(+1);
                e.Handled = true;
                break;
            case Key.Left:
                Navigate(-1);
                e.Handled = true;
                break;
            case Key.Escape:
                if (DoneOverlay.IsVisible)
                    ResetSession();
                else
                    Close();
                e.Handled = true;
                break;
        }
    }

    private async void SaveAndAdvance()
    {
        if (_currentBitmap == null || _currentIndex < 0) return;

        var cropRect = CropCanvas.GetCropRect();
        if (cropRect == SKRectI.Empty)
        {
            ShowError("Crop area is empty — try zooming in.");
            return;
        }

        var sourcePath      = _imagePaths[_currentIndex];
        var outputFileName  = Path.GetFileNameWithoutExtension(sourcePath) + "_cropped.jpg";
        var outputPath      = Path.Combine(_outputFolder!, outputFileName);
        var sourceBitmap    = _currentBitmap;

        try
        {
            await Task.Run(() =>
            {
                var cropped = _cropService.Crop(sourceBitmap, cropRect);
                _cropService.Save(cropped, outputPath, 95);
                cropped.Dispose();
            });
            _savedCount++;
        }
        catch (Exception ex)
        {
            ShowError($"Save failed: {ex.Message}");
            return;
        }

        if (_currentIndex < _imagePaths.Count - 1)
        {
            _currentIndex++;
            LoadCurrentImageAsync();
        }
        else
        {
            ShowDone();
        }
    }

    private void Navigate(int delta)
    {
        if (_imagePaths.Count == 0) return;
        int newIndex = _currentIndex + delta;
        if (newIndex < 0 || newIndex >= _imagePaths.Count) return;
        _currentIndex = newIndex;
        LoadCurrentImageAsync();
    }

    private void UpdateStatus()
    {
        if (_imagePaths.Count == 0 || _currentIndex < 0)
        {
            FileNameText.Text = "Drop photos to begin";
            ProgressText.Text = "";
        }
        else
        {
            FileNameText.Text = Path.GetFileName(_imagePaths[_currentIndex]);
            ProgressText.Text = $"{_currentIndex + 1} / {_imagePaths.Count}";
        }
    }

    private void ShowDone()
    {
        var plural  = _savedCount == 1 ? "image" : "images";
        DoneText.Text = $"{_savedCount} {plural} saved to:\n{_outputFolder}";
        DoneOverlay.IsVisible = true;
        Focus();
    }

    private void ResetSession()
    {
        DoneOverlay.IsVisible = false;
        _currentBitmap?.Dispose();
        _imagePaths.Clear();
        _currentIndex = -1;
        _savedCount   = 0;
        _outputFolder = null;
        _currentBitmap = null;
        CropCanvas.Image = null;
        UpdateStatus();
        Focus();
    }

    private void ShowError(string message)
    {
        ErrorText.Text        = message;
        ErrorToast.IsVisible  = true;

        _errorTimer?.Stop();
        _errorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _errorTimer.Tick += (_, _) =>
        {
            ErrorToast.IsVisible = false;
            _errorTimer!.Stop();
        };
        _errorTimer.Start();
    }
}
