// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Alexander Graef <alex@graef.cc>
//
// This file is part of WaveDude Next, a 2D wave simulator.
//
// Based on the work of
//   Jeroen Vleggaar (Huygens Optics), author of Wave Dude 2D, Copyright (C) 2026
//     <https://www.youtube.com/@HuygensOptics>, <https://www.huygensoptics.com>
//   0x23 (Diffraction Limited), author of WaveSimulator2D
//     <https://www.youtube.com/@DiffractionLimited>, <https://github.com/0x23/WaveSimulator2D>
//
// WaveDude Next is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version. It is distributed WITHOUT ANY WARRANTY. See the LICENSE.md file or
// <https://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SkiaSharp;
using WaveDudeNext.Avalonia.Panes;
using WaveDudeNext.Avalonia.Rendering;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Views;

public partial class SceneView : UserControl
{
    public static SceneMode[] SceneModeValues { get; } = Enum.GetValues<SceneMode>();
    public static EdgeMode[] EdgeModeValues { get; } = Enum.GetValues<EdgeMode>();
    public static FuncValueConverter<SceneMode, bool> IsCanvas { get; } = new(mode => mode == SceneMode.Canvas);
    public static FuncValueConverter<SceneMode, bool> IsImage { get; } = new(mode => mode == SceneMode.Image);
    public static FuncValueConverter<byte[]?, string> ImageInfo { get; } = new(bytes =>
    {
        if (bytes is null) return "";
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var codec = SKCodec.Create(stream);
            return codec is null ? "(unreadable)" : string.Format(CultureInfo.CurrentCulture,
                "{0} × {1}, {2:N0} KB", codec.Info.Width, codec.Info.Height, Math.Max(1, bytes.Length / 1024));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException)
        {
            return "(unreadable)";
        }
    });

    public static readonly StyledProperty<bool> SceneImageFileMissingProperty =
        AvaloniaProperty.Register<SceneView, bool>(nameof(SceneImageFileMissing));
    public bool SceneImageFileMissing
    {
        get => GetValue(SceneImageFileMissingProperty);
        private set => SetValue(SceneImageFileMissingProperty, value);
    }

    private readonly DispatcherTimer _pathTimer;
    private Scene? _scene;
    private string? ProjectFolder => this.FindAncestorOfType<PropertiesPane>()?.ProjectFolder;

    public SceneView()
    {
        InitializeComponent();
        _pathTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pathTimer.Tick += (_, _) => RefreshMissingFile();
        DataContextChanged += (_, _) => ObserveScene();
        AttachedToVisualTree += (_, _) => { ObserveScene(); _pathTimer.Start(); };
        DetachedFromVisualTree += (_, _) =>
        {
            _pathTimer.Stop();
            if (_scene is not null) _scene.PropertyChanged -= OnSceneChanged;
            _scene = null;
        };
    }

    private void ObserveScene()
    {
        if (_scene is not null) _scene.PropertyChanged -= OnSceneChanged;
        _scene = DataContext as Scene;
        if (_scene is not null) _scene.PropertyChanged += OnSceneChanged;
        RefreshMissingFile();
    }

    private void OnSceneChanged(object? sender, PropertyChangedEventArgs e) => RefreshMissingFile();

    private void RefreshMissingFile()
    {
        var path = _scene?.ResolveImagePath(ProjectFolder);
        SceneImageFileMissing = !string.IsNullOrEmpty(path) && !File.Exists(path);
    }

    private async void OnBrowseSceneImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Scene scene || TopLevel.GetTopLevel(this) is not { } window) return;
        try
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose a scene image",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.bmp", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.tif", "*.tiff", "*.ico", "*.jxr", "*.wdp"]
                }]
            });
            if (files.Count == 0) return;
            using var stream = await files[0].OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            byte[] bytes = buffer.ToArray();
            SceneImageLoader.Load(bytes); // Validate before changing the project.
            scene.ImageData = bytes;
            scene.Image = files[0].Name;
            scene.ImagePath = files[0].TryGetLocalPath() is { } path
                ? Scene.StoredImagePath(ProjectFolder, path) : "";
            scene.Mode = SceneMode.Image;
        }
        catch (Exception ex) { await ShowErrorAsync("Could not load scene image", ex); }
    }

    private async void OnSaveSceneImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Scene { ImageData: { } bytes } scene || TopLevel.GetTopLevel(this) is not { } window) return;
        try
        {
            // Choose an extension from the encoded bytes, independent of the editable image name.
            using var input = new MemoryStream(bytes, writable: false);
            using var codec = SKCodec.Create(input);
            string extension = codec?.EncodedFormat switch
            {
                SKEncodedImageFormat.Png => "png", SKEncodedImageFormat.Jpeg => "jpg",
                SKEncodedImageFormat.Bmp => "bmp", SKEncodedImageFormat.Gif => "gif",
                SKEncodedImageFormat.Webp => "webp", _ => Path.GetExtension(scene.Image).TrimStart('.')
            };
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save embedded scene image",
                SuggestedFileName = Path.GetFileNameWithoutExtension(scene.Image) + "." + extension,
                DefaultExtension = extension,
                ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("Scene image") { Patterns = ["*." + extension] }]
            });
            if (file is null) return;
            await using var output = await file.OpenWriteAsync();
            output.SetLength(0);
            await output.WriteAsync(bytes);
        }
        catch (Exception ex) { await ShowErrorAsync("Could not save scene image", ex); }
    }

    private void OnRemoveSceneImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Scene scene) return;
        try
        {
            if (scene.ImageData is { } bytes)
            {
                var pixels = SceneImageLoader.Load(bytes);
                scene.Width = pixels.Width;
                scene.Height = pixels.Height;
            }
        }
        catch (Exception ex)
        {
            // An unreadable image must still be removable; keep the existing canvas dimensions.
            Console.WriteLine(ex);
        }
        scene.RemoveImage();
    }

    private async Task ShowErrorAsync(string title, Exception error)
    {
        Console.WriteLine(error);
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var close = new Button { Content = "Close", HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right };
        var dialog = new Window
        {
            Title = title, Width = 440, SizeToContent = SizeToContent.Height, CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16), Spacing = 16,
                Children = { new TextBlock { Text = error.Message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap }, close }
            }
        };
        close.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(owner);
    }
}
