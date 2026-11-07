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
using WaveDudeNext.Parts;

namespace WaveDudeNext.Avalonia.Converters;

/// <summary>
/// One read-only value of a <see cref="LogicGatePart"/>'s layout as text: the first value is the gate, the others are its
/// position, angle and design inputs, bound only so the text follows them. The parameter picks the value: "Wavelength"
/// ("≈ 32.1 px", the design frequency's) and "QuarterWave" (the core's quarter-wave coating), "CoreWidth", "Gap",
/// "CouplerLength" and "Length" give pixels ("8 px"), "InputA", "InputB", "OutputUpper" and "OutputLower" a port's canvas
/// pixel ("(131, 103)"). "–" without a design (a core index not above the cladding's).
/// </summary>
public sealed class LogicGateLayoutConverter : IMultiValueConverter
{
	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count < 1 || values[0] is not LogicGatePart gate)
		{
			return "–";
		}
		if (parameter as string == "Wavelength")
		{
			return double.IsFinite(gate.Wavelength) && gate.Wavelength > 0.0 ? string.Format(culture, "≈ {0:0.0} px", gate.Wavelength) : "–";
		}
		if (gate.Design is not LogicGateDesign design || gate.Ports() is not { } ports)
		{
			return "–";
		}
		string Pixels(int value) => string.Format(culture, "{0} px", value);
		string Port((int X, int Y) port) => string.Format(culture, "({0}, {1})", port.X, port.Y);
		return (parameter as string) switch
		{
			"QuarterWave" => string.Format(culture, "≈ {0:0.0} px", design.QuarterWaveCoating),
			"CoreWidth" => Pixels(design.CoreWidth),
			"Gap" => Pixels(design.Gap),
			"CouplerLength" => Pixels(design.CouplerLength),
			"Length" => Pixels(design.Length),
			"InputA" => Port(ports[0]),
			"InputB" => Port(ports[1]),
			"OutputUpper" => Port(ports[2]),
			"OutputLower" => Port(ports[3]),
			_ => "–",
		};
	}

	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
