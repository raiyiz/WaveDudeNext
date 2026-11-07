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
using Avalonia.Controls;
using Avalonia.Interactivity;
using WaveDudeNext.Engine;
using WaveDudeNext.Runs;

namespace WaveDudeNext.Avalonia;
public partial class MainWindow
{
    private PreviewQuality _quality = PreviewQuality.Full;
    private byte[] _indices = Array.Empty<byte>();
    private int StoredEvery => _visualizeEvery * _quality.FrameDivisor;
    private void PreviewQuality_Click(object? sender, RoutedEventArgs e)
    {
        _desktop.Quality = (sender as Control)?.Tag?.ToString() ?? "Full";
        ChooseQuality(); SaveDesktop();
        if (_loadedProject is not null) LoadSimulation(_loadedProject);
    }
    private void ChooseQuality()
    {
        _quality = _desktop.Quality switch
        {
            "Economy" => new PreviewQuality(2, 2, FrameFormat.Index),
            "Balanced" => new PreviewQuality(1, 1, FrameFormat.Index),
            "Auto" => FrameMemory.PhysicalBytes() < PreviewDefaults.LowBytes ? new PreviewQuality(2, 2, FrameFormat.Index)
                : FrameMemory.PhysicalBytes() < PreviewDefaults.HighBytes ? new PreviewQuality(1, 1, FrameFormat.Index) : PreviewQuality.Full,
            _ => PreviewQuality.Full
        };
    }
    private byte[]? PreviewEdges()
    {
        if (_overlayEdges is null || _frames is null) return null;
        if (_quality.ResolutionDivisor == 1) return _overlayEdges;
        var edges = new byte[_frames.Width * _frames.Height * 4];
        for (int y = 0; y < _frames.Height; y++)
            for (int x = 0; x < _frames.Width; x++) _overlayEdges.AsSpan((Math.Min(y * _quality.ResolutionDivisor, _engine.Height - 1) * _engine.Width + Math.Min(x * _quality.ResolutionDivisor, _engine.Width - 1)) * 4, 4).CopyTo(edges.AsSpan((y * _frames.Width + x) * 4, 4));
        return edges;
    }
}
