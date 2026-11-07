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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>Portable 16-pixel vectors, including the WPF optical-part drawings.</summary>
public sealed class IconView : Control
{
    public static readonly StyledProperty<string> DrawingProperty =
        AvaloniaProperty.Register<IconView, string>(nameof(Drawing), "Project");
    public static readonly StyledProperty<bool> PlusBadgeProperty = AvaloniaProperty.Register<IconView, bool>(nameof(PlusBadge));
    public bool PlusBadge { get => GetValue(PlusBadgeProperty); set => SetValue(PlusBadgeProperty, value); }
    static IconView() => AffectsRender<IconView>(DrawingProperty, PlusBadgeProperty, TextBlock.ForegroundProperty);
    public string Drawing { get => GetValue(DrawingProperty); set => SetValue(DrawingProperty, value); }
    public IconView() { Width = Height = 16; IsHitTestVisible = false; VerticalAlignment = VerticalAlignment.Center; }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var brush = GetValue(TextBlock.ForegroundProperty);
        if (Shapes.TryGetValue(Drawing, out var stroke)) context.DrawGeometry(null, new Pen(brush, 1), stroke);
        if (Shapes.TryGetValue(Drawing + "Fill", out var fill)) context.DrawGeometry(brush, null, fill);
        if (PlusBadge) context.DrawGeometry(null, new Pen(brush, 1.5), Shapes["PlusBadge"]);
    }
    public static object Label(string drawing, string text) => new StackPanel
    {
        Orientation = Orientation.Horizontal, Spacing = 6,
        Children = { new IconView { Drawing = drawing }, new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center } }
    };
    private static readonly Dictionary<string, Geometry> Shapes = new()
    {
        ["Source"] = Geometry.Parse("M3.5,7.5 A4,4 0 1 1 11.5,7.5 A4,4 0 1 1 3.5,7.5 M7.5,0 V2 M7.5,13 V15 M0,7.5 H2 M13,7.5 H15"),
        ["Probe"] = Geometry.Parse("M4.5,4.5 H10.5 V10.5 H4.5 Z M7.5,0 V2 M7.5,13 V15 M0,7.5 H2 M13,7.5 H15"),
        ["Part"] = Geometry.Parse("M3.5,7.5 A4,4 0 1 1 11.5,7.5 A4,4 0 1 1 3.5,7.5 M5.5,7.5 A2,2 0 1 1 9.5,7.5 A2,2 0 1 1 5.5,7.5 M7.5,0 V2 M7.5,13 V15 M0,7.5 H2 M13,7.5 H15"),
        ["PlusBadge"] = Geometry.Parse("M12.5,10 V15 M10,12.5 H15"),
        ["Project"] = Geometry.Parse("M1.5,2.5 H14.5 V13.5 H1.5 Z M3,8 C4.5,4.5 6.5,4.5 8,8 C9.5,11.5 11.5,11.5 13,8"),
        ["Lens"] = Geometry.Parse("M4,1.5 C7.5,4.5 7.5,11.5 4,14.5 C0.5,11.5 0.5,4.5 4,1.5 Z M13.5,1.5 V14.5 H10.5 C12.5,11.5 12.5,4.5 10.5,1.5 Z"),
        ["LensConvexConvex"] = Geometry.Parse("M8,1.5 C4.5,4.5 4.5,11.5 8,14.5 C11.5,11.5 11.5,4.5 8,1.5 Z"),
        ["LensConvexFlat"] = Geometry.Parse("M10.5,1.5 C3.5,4.5 3.5,11.5 10.5,14.5 Z"),
        ["LensConvexConcave"] = Geometry.Parse("M8,1.5 C3.5,4.5 3.5,11.5 8,14.5 L11,14.5 C7.5,11.5 7.5,4.5 11,1.5 Z"),
        ["LensFlatConvex"] = Geometry.Parse("M5.5,1.5 V14.5 C12.5,11.5 12.5,4.5 5.5,1.5 Z"),
        ["LensFlatFlat"] = Geometry.Parse("M5.5,1.5 V14.5 H10.5 V1.5 Z"),
        ["LensFlatConcave"] = Geometry.Parse("M5.5,1.5 V14.5 H11.5 C9,11.5 9,4.5 11.5,1.5 Z"),
        ["LensConcaveConvex"] = Geometry.Parse("M5,1.5 C8.5,4.5 8.5,11.5 5,14.5 L8,14.5 C12.5,11.5 12.5,4.5 8,1.5 Z"),
        ["LensConcaveFlat"] = Geometry.Parse("M4.5,1.5 C7,4.5 7,11.5 4.5,14.5 H10.5 V1.5 Z"),
        ["LensConcaveConcave"] = Geometry.Parse("M4.5,1.5 C6.5,4.5 6.5,11.5 4.5,14.5 L11.5,14.5 C9.5,11.5 9.5,4.5 11.5,1.5 Z"),
        ["Fiber"] = Geometry.Parse("M1,13.5 C7,13.5 9,5.5 15,5.5 M1,10.5 C7,10.5 9,2.5 15,2.5"),
        ["BraggReflectorFill"] = Geometry.Parse("M2,2 H4 V14 H2 Z M6,2 H8 V14 H6 Z M10,2 H12 V14 H10 Z"),
        ["GrinLens"] = Geometry.Parse("M2.5,2.5 H13.5 V13.5 H2.5 Z M2.5,8 H13.5 M4,6 H12 M4,10 H12 M6,4.5 H10 M6,11.5 H10"),
        ["ZonePlateFill"] = Geometry.Parse("M6,3 H10 V6 H6 Z M6,10 H10 V13 H6 Z M6,0.5 H10 V1.75 H6 Z M6,14.25 H10 V15.5 H6 Z"),
        ["Polygon"] = Geometry.Parse("M5,2.5 L11,2.5 L14,8 L11,13.5 L5,13.5 L2,8 Z"),
        ["Rectangle"] = Geometry.Parse("M2.5,4.5 H13.5 V11.5 H2.5 Z"),
        ["Ellipse"] = Geometry.Parse("M2,8 A6,4.5 0 1 1 14,8 A6,4.5 0 1 1 2,8 Z"),
        ["Triangle"] = Geometry.Parse("M8,2.5 L14,13.5 L2,13.5 Z"),
        ["Outline"] = Geometry.Parse("M2.5,12.5 L5,3.5 L9.5,6.5 L13.5,2.5 L12.5,13.5 Z"),
        ["OpticalLogic"] = Geometry.Parse("M2.5,3.5 H8 A4.5,4.5 0 0 1 8,12.5 H2.5 Z M0,6 H2.5 M0,10 H2.5 M12.5,8 H16"),
        ["XorGate"] = Geometry.Parse("M4,3.5 Q7,8 4,12.5 Q11,12.5 13.5,8 Q11,3.5 4,3.5 Z M2,3.5 Q5,8 2,12.5 M13.5,8 H16"),
        ["AndGate"] = Geometry.Parse("M2.5,3.5 H8 A4.5,4.5 0 0 1 8,12.5 H2.5 Z M12.5,8 H16"),
        ["OrGate"] = Geometry.Parse("M2.5,3.5 Q5.5,8 2.5,12.5 Q10,12.5 13.5,8 Q10,3.5 2.5,3.5 Z M13.5,8 H16"),
        ["AutoBrightness"] = Geometry.Parse("M8,2.5 A5.5,5.5 0 1 1 8,13.5 A5.5,5.5 0 1 1 8,2.5 Z"),
        ["AutoBrightnessFill"] = Geometry.Parse("M8,2.5 A5.5,5.5 0 0 1 8,13.5 Z"),
        ["PreviewResolution"] = Geometry.Parse("M2.5,2.5 H13.5 V13.5 H2.5 Z M6.5,2.5 V13.5 M10.5,2.5 V13.5 M2.5,6.5 H13.5 M2.5,10.5 H13.5"),
        ["PreviewFrames"] = Geometry.Parse("M1.5,3.5 H14.5 V12.5 H1.5 Z M5.5,3.5 V12.5 M10.5,3.5 V12.5"),
        ["PreviewSamples"] = Geometry.Parse("M1.5,14.5 V11.5 H4.5 V8.5 H7.5 V5.5 H10.5 V2.5 H14.5"),
        ["PreviewDither"] = Geometry.Parse("M2.5,2.5 H13.5 V13.5 H2.5 Z"),
        ["PreviewDitherFill"] = Geometry.Parse("M3,3 h2.5 v2.5 h-2.5 Z M8,3 h2.5 v2.5 h-2.5 Z M5.5,5.5 h2.5 v2.5 h-2.5 Z M10.5,5.5 h2.5 v2.5 h-2.5 Z M3,8 h2.5 v2.5 h-2.5 Z M8,8 h2.5 v2.5 h-2.5 Z M5.5,10.5 h2.5 v2.5 h-2.5 Z M10.5,10.5 h2.5 v2.5 h-2.5 Z"),
        ["FrameMemory"] = Geometry.Parse("M2.5,4.5 H13.5 V11.5 H2.5 Z M4.5,11.5 V14 M7,11.5 V14 M9,11.5 V14 M11.5,11.5 V14 M5,7 H11 V9 H5 Z"),
        ["New"] = Geometry.Parse("M3,1.5 H10 L13,4.5 V14.5 H3 Z M10,1.5 V4.5 H13 M5,8 H11 M8,5 V11"),
        ["Open"] = Geometry.Parse("M1.5,12.5 V3.5 H6 L8,5.5 H14.5 L12,12.5 Z M1.5,12.5 L4,7 H14"),
        ["Save"] = Geometry.Parse("M2,2 H12 L14,4 V14 H2 Z M5,2 V6 H11 V2 M5,14 V9 H11 V14"),
        ["Play"] = Geometry.Parse("M4,2 L13,8 L4,14 Z"),
        ["Pause"] = Geometry.Parse("M5,2 V14 M11,2 V14"),
        ["Reset"] = Geometry.Parse("M13,5 A6,6 0 1 0 14,9 M13,1 V5 H9"),
        ["Apply"] = Geometry.Parse("M2,8 L6,12 L14,3"),
        ["Undo"] = Geometry.Parse("M6,3 L2,7 L6,11 M2,7 H9 Q14,7 14,13"),
        ["Redo"] = Geometry.Parse("M10,3 L14,7 L10,11 M14,7 H7 Q2,7 2,13"),
        ["Duplicate"] = Geometry.Parse("M5,5 H14 V14 H5 Z M2,11 V2 H11"),
        ["Delete"] = Geometry.Parse("M2,4 H14 M6,2 H10 M4,4 L5,14 H11 L12,4 M7,6 V12 M9,6 V12"),
        ["Up"] = Geometry.Parse("M3,10 L8,5 L13,10"),
        ["Down"] = Geometry.Parse("M3,6 L8,11 L13,6"),
        ["Export"] = Geometry.Parse("M2,6 V14 H14 V6 M8,10 V1 M4,5 L8,1 L12,5"),
        ["Exit"] = Geometry.Parse("M7,2 H2 V14 H7 M6,8 H15 M11,4 L15,8 L11,12"),
        ["Palette"] = Geometry.Parse("M8,2 C0,2 0,14 7,14 Q10,14 9,11 Q8,9 12,9 C17,9 15,2 8,2 Z M5,5 H6 M9,4 H10 M12,6 H13 M3,8 H4"),
        ["First"] = Geometry.Parse("M3,2 V14 M13,2 L5,8 L13,14 Z"),
        ["Previous"] = Geometry.Parse("M12,2 L3,8 L12,14 Z"),
        ["Next"] = Geometry.Parse("M4,2 L13,8 L4,14 Z"),
        ["Last"] = Geometry.Parse("M13,2 V14 M3,2 L11,8 L3,14 Z"),
    };
}
