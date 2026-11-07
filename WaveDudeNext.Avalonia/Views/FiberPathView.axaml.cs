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
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;

using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Views;

/// <summary>
/// Editor of the node list of a <see cref="FiberPart"/> path (2026-10-22): one block per node with the node, its
/// in- and out-handle offsets and the Smooth flag; add and remove. The data context is the part; the list is its
/// <see cref="FiberPart.Nodes"/>. Every control is in the Tab cycle (the keyboard-first rule).
/// </summary>
public partial class FiberPathView : UserControl
{
	/// <summary>How far a new node is put beyond the last one, in canvas pixels; its handles get a third of that.</summary>
	private const int NewNodeStep = 60;

	public FiberPathView()
	{
		InitializeComponent();
	}

	/// <summary>The edited list: the fibre's nodes, or null for another data context.</summary>
	private ObservableCollection<FiberNode>? Nodes => (DataContext as FiberPart)?.Nodes;

	/// <summary>
	/// Appends a node a step beyond the last one in the direction the path leaves it (against its in-handle, else
	/// from the node before, else to the right), with handles a third of the step long on both sides of the new
	/// segment, so it can be taken hold of; the last node's out-handle is only set when it had none. The first node
	/// goes to the centre.
	/// </summary>
	private void OnAddNode(object sender, RoutedEventArgs e)
	{
		if (Nodes is not { } nodes)
		{
			return;
		}
		FiberNode? last = nodes.LastOrDefault();
		if (last == null)
		{
			nodes.Add(new FiberNode());
			return;
		}
		(double dx, double dy) = Direction(-last.InX, -last.InY)
			?? (nodes.Count > 1 ? Direction(last.X - nodes[^2].X, last.Y - nodes[^2].Y) : null)
			?? (1.0, 0.0);
		int third = NewNodeStep / 3;
		var node = new FiberNode
		{
			X = last.X + (int)Math.Round(dx * NewNodeStep),
			Y = last.Y + (int)Math.Round(dy * NewNodeStep),
			InX = -(int)Math.Round(dx * third),
			InY = -(int)Math.Round(dy * third),
		};
		if (last.OutX == 0 && last.OutY == 0)
		{
			last.OutX = (int)Math.Round(dx * third);
			last.OutY = (int)Math.Round(dy * third);
		}
		nodes.Add(node);
	}

	private void OnRemoveNode(object sender, RoutedEventArgs e)
	{
		if ((sender as Control)?.DataContext is FiberNode node)
		{
			Nodes?.Remove(node);
		}
	}

	/// <summary>The direction (x, y) as a unit vector, or null for a zero vector.</summary>
	private static (double X, double Y)? Direction(double x, double y)
	{
		double length = Math.Sqrt(x * x + y * y);
		return length > 0.0 ? (x / length, y / length) : null;
	}
}
