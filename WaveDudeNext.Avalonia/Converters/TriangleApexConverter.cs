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
using Avalonia.Data.Converters;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Converters;

/// <summary>
/// The apex angle of a triangle part as text ("60°"): 180° minus the two base angles the type gives, computed on a
/// throw-away part so the model's own rule (<see cref="TrianglePart.EffectiveAngles"/>) decides. Inputs: the type, angle A,
/// angle B; re-evaluated on each edit.
/// </summary>
public sealed class TriangleApexConverter : IMultiValueConverter
{
	/// <inheritdoc/>
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count < 3 || values[0] is not TriangleType type || values[1] is not double a || values[2] is not double b)
		{
			return null;
		}
		var probe = new TrianglePart { Type = type, AngleA = a, AngleB = b };
		return string.Format(culture, "{0:0.#}°", probe.ApexAngle);
	}

	/// <inheritdoc/>
	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
