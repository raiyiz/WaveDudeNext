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

using Avalonia;
using WaveDudeNext.Rendering;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;

namespace WaveDudeNext.Avalonia.Views;

/// <summary>Editor for one <see cref="WaveDudeNext.Model.DisplayLayer"/>; used twice by <see cref="DisplayView"/>.</summary>
public partial class DisplayLayerView : UserControl
{
	/// <summary>Identifies the <see cref="Kind"/> property.</summary>
	public static readonly StyledProperty<ColorSchemeKind> KindProperty = AvaloniaProperty.Register<DisplayLayerView, ColorSchemeKind>(nameof(Kind), ColorSchemeKind.Unspecified);

	/// <summary>Which output this view edits (set by the display view), so the scheme picker offers the matching schemes.</summary>
	public ColorSchemeKind Kind
	{
		get => GetValue(KindProperty);
		set => SetValue(KindProperty, value);
	}

	public DisplayLayerView()
	{
		InitializeComponent();
	}
}
