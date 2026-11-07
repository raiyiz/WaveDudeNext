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
using WaveDudeNext.Model;
using WaveDudeNext.Rendering;

namespace WaveDudeNext.Avalonia.Controls;

public static class EnumItems
{
    public static ColorSchemeKind[] ColorSchemeKind { get; } = Enum.GetValues<ColorSchemeKind>();
    public static CoatingProfile[] CoatingProfile { get; } = Enum.GetValues<CoatingProfile>();
    public static FiberEnds[] FiberEnds { get; } = Enum.GetValues<FiberEnds>();
    public static ProbeBoxSize[] ProbeBoxSize { get; } = Enum.GetValues<ProbeBoxSize>();
    public static ProbeKind[] ProbeKind { get; } = Enum.GetValues<ProbeKind>();
    public static ProbeLayer[] ProbeLayer { get; } = Enum.GetValues<ProbeLayer>();
    public static ProbeScaleMode[] ProbeScaleMode { get; } = Enum.GetValues<ProbeScaleMode>();
    public static ProbeValueScale[] ProbeValueScale { get; } = Enum.GetValues<ProbeValueScale>();
    public static PulseShape[] PulseShape { get; } = Enum.GetValues<PulseShape>();
    public static SourceMode[] SourceMode { get; } = Enum.GetValues<SourceMode>();
    public static TriangleType[] TriangleType { get; } = Enum.GetValues<TriangleType>();
}
