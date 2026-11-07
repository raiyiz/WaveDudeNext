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
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Converters;

/// <summary>
/// The description of a part's kind for the top of its form (2026-10-29, the user's choice): the string table's
/// <c>Kind.{type}</c> entry, the one the Add part menu shows as the entry's tooltip; "" for a kind without one. The
/// second input is the language, so the text is re-read on a switch.
/// </summary>
public sealed class KindDescriptionConverter : IMultiValueConverter, IValueConverter
{
    public object Convert(object? value, Type type, object? parameter, CultureInfo culture) =>
        value is OpticalPart part ? Application.Current?.TryFindResource("Kind." + part.GetType().Name) ?? "" : "";
    public object ConvertBack(object? value, Type type, object? parameter, CultureInfo culture) => throw new NotSupportedException();

	/// <inheritdoc/>
	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
		values is [OpticalPart part, ..] && Application.Current?.TryFindResource("Kind." + part.GetType().Name) is string text ? text : "";

	/// <inheritdoc/>
	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
