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
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Platform.Storage;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia;
public partial class MainWindow
{
    private void InitializeDrop()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            e.DragEffects = e.DataTransfer.TryGetFiles()?.Any() == true || e.DataTransfer.TryGetText() is { } text && ProjectClipboard.Read(text) is not null ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        });
        AddHandler(DragDrop.DropEvent, async (_, e) =>
        {
            e.Handled = true;
            try
            {
                var path = e.DataTransfer.TryGetFiles()?.FirstOrDefault()?.TryGetLocalPath();
                if (path is not null)
                {
                    if (Path.GetExtension(path).Equals(".wdproj", StringComparison.OrdinalIgnoreCase))
                    { if (await ConfirmProjectReplacement()) InstallProject(Project.Load(path), path); }
                    else if (LegacyProject.IsLegacyFile(path))
                    {
                        if (!await ConfirmProjectReplacement()) return;
                        var notes = new System.Collections.Generic.List<string>();
                        InstallProject(LegacyProject.Import(path, notes, out string importedName), null); _hasPendingChanges = true;
                        if (notes.Count > 0) await ShowText("Import notes", string.Join("\n", notes));
                    }
                    else if (_loadedProject is { } project)
                    {
                        _ = Rendering.SceneImageLoader.Load(path);
                        project.Scene.ImageData = File.ReadAllBytes(path);
                        project.Scene.ImagePath = path; project.Scene.Image = Path.GetFileName(path); project.Scene.Mode = SceneMode.Image;
                        FinishObjectEdit(project.Scene);
                    }
                    return;
                }
                if (_loadedProject is not { } target || e.DataTransfer.TryGetText() is not { } xml || ProjectClipboard.Read(xml) is not { } clip) return;
                var row = e.Source as TreeViewItem ?? (e.Source as Control)?.FindAncestorOfType<TreeViewItem>();
                object? destination = row?.Tag ?? _selectedObject;
                bool moving = ProjectTreeControl.DraggedObjects.Length > 0 && !e.KeyModifiers.HasFlag(KeyModifiers.Control);
                var pasted = moving ? (System.Collections.Generic.IReadOnlyList<object>)ProjectTreeControl.DraggedObjects : ProjectClipboard.Paste(target, clip, destination);
                if (moving && row is not null && destination is not null)
                {
                    foreach (var item in pasted)
                    {
                        switch (item)
                        {
                            case Source source: target.Move(source, destination is Source s ? target.Sources.IndexOf(s) : target.Sources.Count - 1); break;
                            case OpticalPart part: target.Move(part, destination is OpticalPart p ? target.Parts.IndexOf(p) : target.Parts.Count - 1); break;
                            case Probe probe: target.Move(probe, destination is Probe q ? target.Probes.IndexOf(q) : target.Probes.Count - 1); break;
                            case WaveDudeNext.Rendering.ColorScheme scheme: target.Move(scheme, destination is WaveDudeNext.Rendering.ColorScheme c ? target.ColorSchemes.IndexOf(c) : target.ColorSchemes.Count - 1); break;
                        }
                    }
                }
                if (pasted.Count == 0) return;
                foreach (var canvas in new[] { FieldCanvas, IntensityCanvas })
                {
                    if (!canvas.IsAttachedToVisualTree()) continue;
                    var pointer = e.GetPosition(canvas);
                    if (!new global::Avalonia.Rect(canvas.Bounds.Size).Contains(pointer)) continue;
                    var point = canvas.CanvasPoint(pointer);
                    ProjectClipboard.PlaceAt(pasted, (int)point.X, (int)point.Y, canvas.CanvasWidth, canvas.CanvasHeight);
                    break;
                }
                FinishObjectEdit(pasted[^1]);
            }
            catch (Exception ex) { StatusText.Text = "Could not drop item: " + ex.Message; }
        });
    }
}
