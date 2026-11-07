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
using System.Globalization;
using Avalonia.Data.Converters;

namespace WaveDudeNext.Avalonia.Converters;

/// <summary>Preserves the enabled-state rules of the WPF property editors.</summary>
public sealed class ConditionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string[] rules = (parameter as string ?? "True").Split(';');
        bool result = bool.Parse(rules[0]);
        foreach (string rule in rules[1..])
        {
            var pair = rule.Split('=');
            if (string.Equals(value?.ToString(), pair[0], StringComparison.OrdinalIgnoreCase))
                result = bool.Parse(pair[1]);
        }
        return result;
    }
    public object ConvertBack(object? value, Type type, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
