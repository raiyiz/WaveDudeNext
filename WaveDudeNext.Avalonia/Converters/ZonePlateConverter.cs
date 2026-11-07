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
/// One read-only value of a <see cref="ZonePlatePart"/> as text: the first value is the plate, the others the inputs the
/// value depends on, bound only so the text follows them. The parameter picks it: "Wavelength" ("≈ 32.0 px"), "Zones"
/// (the zone count, "8") or "OuterZone" (the outermost whole zone's width, "≈ 30.6 px"); "–" without a wavelength.
/// </summary>
public sealed class ZonePlateConverter : IMultiValueConverter
{
	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count < 1 || values[0] is not ZonePlatePart plate || !double.IsFinite(plate.Wavelength) || plate.Wavelength <= 0.0)
		{
			return "–";
		}
		return (parameter as string) switch
		{
			"Wavelength" => string.Format(culture, "≈ {0:0.0} px", plate.Wavelength),
			"Zones" => plate.ZoneCount.ToString(culture),
			"OuterZone" => string.Format(culture, "≈ {0:0.0} px", plate.OuterZoneWidth),
			_ => "–",
		};
	}

	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
