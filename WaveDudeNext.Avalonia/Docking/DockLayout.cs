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

namespace WaveDudeNext.Avalonia.Docking;

public enum DockSide { Tab, Left, Right, Top, Bottom }

/// <summary>Serializable split/tab tree. Pane IDs are independent of control instances.</summary>
public sealed class DockNode
{
    public List<string> Items { get; set; } = new();
    public string? Active { get; set; }
    public DockNode? First { get; set; }
    public DockNode? Second { get; set; }
    public bool Vertical { get; set; }
    public double Ratio { get; set; } = 0.5;
    public bool IsSplit => First is not null || Second is not null;
    public static DockNode Group(params string[] ids) => new() { Items = new(ids), Active = ids.Length > 0 ? ids[0] : null };
    public static DockNode Split(DockNode first, DockNode second, bool vertical = false, double ratio = 0.5) =>
        new() { First = first, Second = second, Vertical = vertical, Ratio = ratio };
}
public sealed class FloatingDockLayout
{
    public DockNode? Root { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public double Width { get; set; } = 640;
    public double Height { get; set; } = 480;
}
public sealed class DockLayout
{
    public DockNode? Root { get; set; }
    public List<string> Hidden { get; set; } = new();
    public List<FloatingDockLayout> Floating { get; set; } = new();
}
