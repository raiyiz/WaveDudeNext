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
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WaveDudeNext.Rendering;

namespace WaveDudeNext.Avalonia.Converters;

public sealed class ColorSchemeBrushConverter : IValueConverter
{
    public object Convert(object? value, Type type, object? parameter, CultureInfo culture)
    {
        var brush = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative) };
        if (value is IEnumerable<ColorStop> stops)
            foreach (var stop in stops) brush.GradientStops.Add(new GradientStop(Color.FromRgb(stop.Color.R, stop.Color.G, stop.Color.B), stop.Position));
        return brush;
    }
    public object ConvertBack(object? value, Type type, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
public sealed class RgbBrushConverter : IValueConverter
{
    public object Convert(object? value, Type type, object? parameter, CultureInfo culture) =>
        value is Rgb rgb ? new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B)) : Brushes.Transparent;
    public object ConvertBack(object? value, Type type, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
