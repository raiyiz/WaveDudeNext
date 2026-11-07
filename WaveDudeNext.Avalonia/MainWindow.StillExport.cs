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
using System.Threading.Tasks;
using WaveDudeNext.Export;
using WaveDudeNext.Model;
using WaveDudeNext.Rendering;
using WaveDudeNext.Runs;

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private async Task<(int Width, int Height, int[] Pixels)?> RenderCurrentFrameForExport(bool intensity)
    {
        if (_frames is not { Count: > 0 } frames || _loadedProject is not { } project) return null;
        FrameStore? generated = null;
        try
        {
            if (!_quality.IsFull)
            {
                generated = await PrepareExportFrames(true);
                if (generated is null) return null;
                frames = generated;
            }
            int index = Math.Clamp(_viewFrame, 0, frames.Count - 1);
            int width = frames.Width, height = frames.Height;
            var samples = new Half[width * height];
            var pixels = new int[samples.Length];
            var layer = intensity ? FrameLayer.Intensity : FrameLayer.Field;
            if (!frames.CopyLayer(index, layer, samples.AsSpan())) throw new InvalidOperationException("The frame is no longer available.");
            var display = intensity ? project.Display.Intensity : project.Display.Field;
            var scheme = ColorSchemeStore.Find(project.ColorSchemes, display.ColorScheme) ?? ColorSchemeStore.Find(_builtInSchemes, display.ColorScheme);
            var lut = scheme is null ? ColorLut.Grey : ColorLut.Build(scheme, display.BlackLevel);
            FrameRenderer.Render(samples, width, height, lut, ((IFrameSource)frames).KindOf(layer), (float)display.Brightness, pixels);
            if (display.OverlayInExport && _overlayEdges is not null) GlowingEdges.BlendAdditive(pixels, width, height, _overlayEdges, display.OverlayGain);
            Rendering.ExportDecoration.Draw(project, generated is null ? _probeTraces : _exportTraces, intensity ? ProbeLayer.Intensity : ProbeLayer.Field,
                width, height, index, index * _visualizeEvery, _runSteps, pixels);
            return (width, height, pixels);
        }
        finally { generated?.Dispose(); _exportTraces.Clear(); }
    }
}
