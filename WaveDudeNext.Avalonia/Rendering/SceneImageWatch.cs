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
using System.IO;
using Avalonia.Threading;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Rendering;

/// <summary>Keeps an embedded scene image current when its original file changes.</summary>
public sealed class SceneImageWatch : IDisposable
{
    private readonly Action<string> _report;
    private readonly DispatcherTimer _debounce;
    private FileSystemWatcher? _watcher;
    private Scene? _scene;
    private string? _folder;
    private int _generation;
    private int _retries;

    public SceneImageWatch(Action<string> report)
    {
        _report = report;
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            if (!Refresh() && ++_retries < 8) _debounce.Start();
        };
    }

    public void Attach(Scene scene, string? folder, bool refresh = true)
    {
        Dispose();
        _scene = scene;
        _folder = folder;
        // Old projects may know only the embedded image's name.
        if (scene.ImagePath.Length == 0 && scene.ImageData is not null && scene.Image.Length > 0 && folder is not null)
        {
            foreach (var subfolder in new[] { "", "scene_images" })
            {
                string candidate = Path.Combine(folder, subfolder, scene.Image);
                if (!File.Exists(candidate)) continue;
                scene.ImagePath = Scene.StoredImagePath(folder, candidate);
                break;
            }
        }
        scene.PropertyChanged += OnSceneChanged;
        Arm();
        if (refresh) Refresh();
    }

    private void OnSceneChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Scene.ImagePath) or nameof(Scene.Mode) or nameof(Scene.ImageData))
            Arm();
    }

    private void Arm()
    {
        _generation++;
        _debounce.Stop();
        _watcher?.Dispose();
        _watcher = null;
        if (_scene is not { ImageData: not null }) return;
        string path = _scene.ResolveImagePath(_folder);
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
        try
        {
            var watcher = new FileSystemWatcher(directory, Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            _watcher = watcher;
            int generation = _generation;
            FileSystemEventHandler changed = (_, _) => Dispatcher.UIThread.Post(() =>
            {
                if (generation != _generation) return;
                _retries = 0;
                _debounce.Stop();
                _debounce.Start();
            });
            watcher.Changed += changed;
            watcher.Created += changed;
            watcher.Deleted += changed;
            watcher.Renamed += (sender, args) => changed(sender, args);
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            _watcher?.Dispose();
            _watcher = null;
            _report($"Could not watch scene image: {ex.Message}");
        }
    }

    private bool Refresh()
    {
        if (_scene is not { ImageData: not null } scene) return true;
        string path = scene.ResolveImagePath(_folder);
        if (path.Length == 0 || !File.Exists(path)) return true;
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.AsSpan().SequenceEqual(scene.ImageData)) return true;
            SceneImageLoader.Load(bytes); // Keep the last valid image if an editor is still writing.
            scene.ImageData = bytes;
            scene.Image = Path.GetFileName(path);
            _report("Scene image updated from disk. Apply Changes to update the simulation.");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _report($"Could not refresh scene image: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        _generation++;
        _debounce.Stop();
        _watcher?.Dispose();
        _watcher = null;
        if (_scene is not null) _scene.PropertyChanged -= OnSceneChanged;
        _scene = null;
    }
}
