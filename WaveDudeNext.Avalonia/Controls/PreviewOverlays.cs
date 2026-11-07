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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>View-only overlay choices. Never disables a solver object or a probe recording.</summary>
public sealed class PreviewOverlays
{
    public bool Drawings { get; set; } = true;
    public bool Sources { get; set; } = true;
    public bool Parts { get; set; } = true;
    public bool ProbeMarkers { get; set; } = true;
    public bool Labels { get; set; } = true;
    public bool ProbeGraphs { get; set; } = true;
    public bool Edges { get; set; } = true;
    [JsonIgnore] public HashSet<object> HiddenObjects { get; } = new();
    [JsonIgnore] public HashSet<Probe> HiddenGraphs { get; } = new();
    public bool ShowsObject(object? item) => Drawings && item is not null && !HiddenObjects.Contains(item) && item switch
    { Source => Sources, OpticalPart => Parts, Probe => ProbeMarkers, _ => false };
    public bool ShowsGraph(Probe probe) => Drawings && ProbeGraphs && !HiddenGraphs.Contains(probe);
    public void ShowAll()
    {
        Drawings = Sources = Parts = ProbeMarkers = Labels = ProbeGraphs = Edges = true;
        HiddenObjects.Clear(); HiddenGraphs.Clear();
    }
}
