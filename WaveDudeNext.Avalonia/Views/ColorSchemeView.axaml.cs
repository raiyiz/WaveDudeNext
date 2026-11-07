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
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Controls;


using WaveDudeNext.Model;
using WaveDudeNext.Rendering;
using WaveDudeNext.Avalonia.Panes;
using WaveDudeNext.Avalonia.Rendering;

namespace WaveDudeNext.Avalonia.Views;

/// <summary>
/// Editor of one project colour scheme (2026-10-13): name, kind, category, the gradient, and one row per
/// stop with its position (0..1), a swatch and the colour as <c>#RRGGBB</c>; stops can be added (in the
/// widest gap) and removed, and a sampled 255-colour table can be reduced to the author's nine. Edits
/// reach the preview through the project watcher.
/// </summary>
public partial class ColorSchemeView : UserControl
{
	private bool _applyingBase;

	public ColorSchemeView()
	{
		InitializeComponent();
		DataContextChanged += (_, _) => FillBasedOn();
		AttachedToVisualTree += (_, _) => FillBasedOn();
	}

	private ColorScheme? Scheme => DataContext as ColorScheme;

	/// <summary>The "based on" choices: the shared schemes (user folder, built-in) and the project's other schemes.</summary>
	private void FillBasedOn()
	{
		_applyingBase = true;
		try
		{
			var choices = new List<ColorScheme>();
			Project? project = FindPane()?.Project;
			if (project != null)
			{
				choices.AddRange(project.ColorSchemes.Where(s => !ReferenceEquals(s, Scheme)));
			}
			choices.AddRange(ColorSchemeStore.LoadBuiltIn(null));
			basedOn.ItemsSource = choices;
			basedOn.SelectedItem = null;
		}
		finally
		{
			_applyingBase = false;
		}
	}

    private PropertiesPane? FindPane() => this.FindAncestorOfType<PropertiesPane>();

	/// <summary>Copies the chosen scheme into this one (stops, kind, category, description); the choice stays shown until the next pick.</summary>
	private void OnBasedOnChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_applyingBase || Scheme is not { } scheme || basedOn.SelectedItem is not ColorScheme chosen)
		{
			return;
		}
		scheme.SetStops(chosen.Stops);
		scheme.Kind = chosen.Kind;
		scheme.Description = "copy of scheme '" + chosen.Name + "'";
	}

	private void OnAddStop(object sender, RoutedEventArgs e) => Scheme?.AddStop();

	private void OnRemoveStop(object sender, RoutedEventArgs e)
	{
		if ((sender as Control)?.DataContext is ColorStop stop)
		{
			Scheme?.RemoveStop(stop);
		}
	}
}
