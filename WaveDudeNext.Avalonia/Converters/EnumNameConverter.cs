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

using System.ComponentModel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Data.Converters;

namespace WaveDudeNext.Avalonia.Converters;

/// <summary>
/// Shows an enum value as words: the string table's <c>Enum.Type.Member</c> entry, else a [Description], else
/// <c>SmoothedSquare</c> becomes "Smoothed square". As a multi-value converter it takes the value and, as a
/// second input, the current language, so a bound text is re-read on a language switch (the Kind combo kept
/// "Intensiteit" after a switch back to English, 2026-10-15).
/// </summary>
public sealed partial class EnumNameConverter : IValueConverter, IMultiValueConverter
{
	/// <inheritdoc/>
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
		values.Count > 0 ? Convert(values[0], targetType, parameter, culture) : null;

	/// <inheritdoc/>
	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();

	[GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
	private static partial Regex WordBoundary();

	/// <inheritdoc/>
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return null;
		}
		if (value is Enum member)
		{
			// the string table ("Enum.Type.Member") first, then a [Description] on the member (ProbeMeasurement.IntegratedValue reads "Integrated value")
			if (Application.Current?.TryFindResource($"Enum.{member.GetType().Name}.{member}") is string localized)
			{
				return localized;
			}
			if (member.GetType().GetField(member.ToString()) is { } field && field.GetCustomAttribute<DescriptionAttribute>() is { } description)
			{
				return description.Description;
			}
		}
		string[] words = WordBoundary().Split(value.ToString() ?? "");
		return string.Join(' ', words.Select((w, i) => i == 0 ? w : w.ToLowerInvariant()));
	}

	/// <inheritdoc/>
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
