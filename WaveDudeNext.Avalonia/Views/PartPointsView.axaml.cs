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

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;

using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Views;

/// <summary>
/// Editor of the point list of an <see cref="OutlinePart"/>: one row per point, add and remove (a fibre's path has
/// its own editor, <see cref="FiberPathView"/>). The data context is the part; the list is its <c>Points</c>. Every
/// control is in the Tab cycle (the keyboard-first rule).
/// </summary>
public partial class PartPointsView : UserControl
{
	/// <summary>Identifies the <see cref="HintText"/> property.</summary>
	public static readonly StyledProperty<string> HintTextProperty = AvaloniaProperty.Register<PartPointsView, string>(nameof(HintText), "");

	/// <summary>How far a new point is put from the last one, in canvas pixels.</summary>
	private const int NewPointStep = 20;

	public PartPointsView()
	{
		InitializeComponent();
	}

	/// <summary>The line above the list saying what the points are (an outline, a path); set by the kind's form.</summary>
	public string HintText
	{
		get => GetValue(HintTextProperty);
		set => SetValue(HintTextProperty, value);
	}

	/// <summary>The edited list: the outline's points, or null for another data context.</summary>
	private ObservableCollection<PartPoint>? Points => (DataContext as OutlinePart)?.Points;

	/// <summary>Appends a point a step to the right of the last one (at the centre for an empty list).</summary>
	private void OnAddPoint(object sender, RoutedEventArgs e)
	{
		if (Points is not { } points)
		{
			return;
		}
		PartPoint? last = points.LastOrDefault();
		points.Add(new PartPoint { X = (last?.X ?? -NewPointStep) + NewPointStep, Y = last?.Y ?? 0 });
	}

	private void OnRemovePoint(object sender, RoutedEventArgs e)
	{
		if ((sender as Control)?.DataContext is PartPoint point)
		{
			Points?.Remove(point);
		}
	}
}
