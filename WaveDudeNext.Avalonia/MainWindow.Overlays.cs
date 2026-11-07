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

using System.Collections.Generic;
using Avalonia.Controls;
using WaveDudeNext.Avalonia.Controls;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private readonly List<PreviewOverlayBar> _overlayBars = new();
    private void InstallOverlayBar(Border host, SceneCanvas canvas, PreviewOverlays settings, string id)
    {
        canvas.Overlays = settings;
        canvas.OverlayVisibilityChanged += RefreshOverlayVisibility;
        var grid = (Grid)host.Child!;
        var bar = new PreviewOverlayBar(canvas, () => _loadedProject, RefreshOverlayVisibility, id, () => AutoPreviewBrightness(canvas.Layer));
        grid.Children.RemoveAt(0); grid.Children.Insert(0, bar);
        _overlayBars.Add(bar);
    }
    private void RefreshOverlayVisibility()
    {
        foreach (var bar in _overlayBars) bar.SynchronizeState();
        FieldCanvas.InvalidateVisual(); IntensityCanvas.InvalidateVisual();
        if (_frames is { Count: > 0 }) ShowStoredFrame(_viewFrame);
        SaveDesktop();
    }
    private void ShowProbeOverSimulation(Probe probe)
    {
        var canvas = probe.Layer == ProbeLayer.Field ? FieldCanvas : IntensityCanvas;
        canvas.Overlays.Drawings = true; canvas.Overlays.ProbeGraphs = true; canvas.Overlays.HiddenGraphs.Remove(probe);
        RefreshOverlayVisibility();
    }
    private void HideProbeOverSimulation(Probe probe)
    {
        FieldCanvas.Overlays.HiddenGraphs.Add(probe); IntensityCanvas.Overlays.HiddenGraphs.Add(probe);
        RefreshOverlayVisibility();
    }
}
