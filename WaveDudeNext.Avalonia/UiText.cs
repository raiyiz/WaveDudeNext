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
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace WaveDudeNext.Avalonia;

/// <summary>Localised desktop labels shared by XAML, docking chrome and dialogs.</summary>
internal static class UiText
{
    public static string Text(string english)
    {
        string key = "Shell." + (english.Contains('_') ? "Access" : "") + new string(english.Where(char.IsLetterOrDigit).ToArray())
            + (english.EndsWith('…') || english.EndsWith("...", StringComparison.Ordinal) ? "More" : "");
        return Application.Current?.TryFindResource(key, out var value) == true && value is string text ? text : english;
    }
    public static string Format(string english, params object?[] values) => string.Format(CultureInfo.CurrentCulture, Text(english), values);
}
