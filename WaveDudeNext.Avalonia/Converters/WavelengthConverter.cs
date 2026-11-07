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
/// The source's estimated wavelength as text: values are the source level, the simulation's wave speed and
/// source frequency scale, and whether the 4th-order Laplacian is on (<see cref="Simulation.Wavelength"/>).
/// "≈ 16,0 px" in the current culture; with the 9-point stencil "(nominal)" is appended, because that
/// stencil's speed varies with the frequency; "–" when the inputs do not give a wavelength.
/// </summary>
public sealed class WavelengthConverter : IMultiValueConverter
{
	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count < 4 || values[0] is not double level || values[1] is not double waveSpeed
			|| values[2] is not double scale || values[3] is not bool fourthOrder)
		{
			return "\u2013";
		}
		double wavelength = Simulation.Wavelength(level, waveSpeed, scale);
		if (double.IsNaN(wavelength) || double.IsInfinity(wavelength))
		{
			return "\u2013";
		}
		string text = string.Format(culture, "\u2248 {0:0.0} px", wavelength);
		if (fourthOrder)
		{
			return text;
		}
		// the 9-point stencil disperses: the word from the string table (a fifth input, the language, re-reads it on a switch)
		return text + " " + (Application.Current?.TryFindResource("WavelengthNominal") as string ?? "(nominal)");
	}

	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
