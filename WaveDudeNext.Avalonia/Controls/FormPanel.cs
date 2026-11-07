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
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>
/// Two-column form layout: children are taken in pairs, label left and editor right, one pair
/// per row. The label column is as wide as the widest label (at least <see cref="LabelWidth"/>),
/// the editor column takes the rest. A child with <see cref="FullWidthProperty"/> set occupies a
/// row of its own across both columns.
/// </summary>
public sealed class FormPanel : Panel
{
    public static readonly StyledProperty<double> LabelWidthProperty =
        AvaloniaProperty.Register<FormPanel, double>(nameof(LabelWidth), 110.0);
    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<FormPanel, double>(nameof(RowSpacing), 4.0);
    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<FormPanel, double>(nameof(ColumnSpacing), 8.0);
    public static readonly AttachedProperty<bool> FullWidthProperty =
        AvaloniaProperty.RegisterAttached<FormPanel, Control, bool>("FullWidth");

    static FormPanel()
    {
        AffectsMeasure<FormPanel>(LabelWidthProperty, RowSpacingProperty, ColumnSpacingProperty);
        FullWidthProperty.Changed.AddClassHandler<Control>((control, _) =>
            (control.Parent as FormPanel)?.InvalidateMeasure());
    }

	/// <summary>Minimum width of the label column.</summary>
	public double LabelWidth
	{
		get => GetValue(LabelWidthProperty);
		set => SetValue(LabelWidthProperty, value);
	}

	/// <summary>Vertical gap between rows.</summary>
	public double RowSpacing
	{
		get => GetValue(RowSpacingProperty);
		set => SetValue(RowSpacingProperty, value);
	}

	/// <summary>Gap between the label and the editor column.</summary>
	public double ColumnSpacing
	{
		get => GetValue(ColumnSpacingProperty);
		set => SetValue(ColumnSpacingProperty, value);
	}

	/// <summary>Gets <see cref="FullWidthProperty"/>.</summary>
	public static bool GetFullWidth(Control element) => element.GetValue(FullWidthProperty);

	/// <summary>Sets <see cref="FullWidthProperty"/>.</summary>
	public static void SetFullWidth(Control element, bool value) => element.SetValue(FullWidthProperty, value);

	/// <inheritdoc/>
	protected override Size MeasureOverride(Size availableSize)
	{
		// first pass: labels at their natural width
		double labelWidth = LabelWidth;
		foreach ((Control? label, _) in Rows())
		{
			if (label != null)
			{
				label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				labelWidth = Math.Max(labelWidth, label.DesiredSize.Width);
			}
		}

		double editorWidth = Math.Max(0, availableSize.Width - labelWidth - ColumnSpacing);
		double height = 0;
		double width = 0;
		int rows = 0;
		foreach ((Control? label, Control? editor) in Rows())
		{
			double rowHeight = 0;
			if (label == null && editor != null)
			{
				editor.Measure(new Size(availableSize.Width, double.PositiveInfinity));
				rowHeight = editor.DesiredSize.Height;
				width = Math.Max(width, editor.DesiredSize.Width);
			}
			else
			{
				if (editor != null)
				{
					editor.Measure(new Size(editorWidth, double.PositiveInfinity));
					rowHeight = editor.DesiredSize.Height;
					width = Math.Max(width, labelWidth + ColumnSpacing + editor.DesiredSize.Width);
				}
				if (label != null)
				{
					rowHeight = Math.Max(rowHeight, label.DesiredSize.Height);
					width = Math.Max(width, labelWidth);
				}
			}
			height += rowHeight;
			rows++;
		}
		if (rows > 1)
		{
			height += (rows - 1) * RowSpacing;
		}
		return new Size(Math.Min(width, availableSize.Width), height);
	}

	/// <inheritdoc/>
	protected override Size ArrangeOverride(Size finalSize)
	{
		double labelWidth = LabelWidth;
		foreach ((Control? label, _) in Rows())
		{
			if (label != null)
			{
				labelWidth = Math.Max(labelWidth, label.DesiredSize.Width);
			}
		}
		double editorX = labelWidth + ColumnSpacing;
		double editorWidth = Math.Max(0, finalSize.Width - editorX);
		double y = 0;
		foreach ((Control? label, Control? editor) in Rows())
		{
			double rowHeight = Math.Max(label?.DesiredSize.Height ?? 0, editor?.DesiredSize.Height ?? 0);
			if (label == null && editor != null)
			{
				editor.Arrange(new Rect(0, y, finalSize.Width, rowHeight));
			}
			else
			{
				label?.Arrange(new Rect(0, y, labelWidth, rowHeight));
				editor?.Arrange(new Rect(editorX, y, editorWidth, rowHeight));
			}
			y += rowHeight + RowSpacing;
		}
		return finalSize;
	}

	/// <summary>Groups the visible children into rows: (label, editor) pairs or (null, full-width child).</summary>
	private IEnumerable<(Control? Label, Control? Editor)> Rows()
	{
		Control? pendingLabel = null;
		foreach (Control? child in Children)
		{
			if (child == null || !child.IsVisible)
			{
				continue;
			}
			if (GetFullWidth(child))
			{
				if (pendingLabel != null)
				{
					yield return (pendingLabel, null);
					pendingLabel = null;
				}
				yield return (null, child);
			}
			else if (pendingLabel == null)
			{
				pendingLabel = child;
			}
			else
			{
				yield return (pendingLabel, child);
				pendingLabel = null;
			}
		}
		if (pendingLabel != null)
		{
			yield return (pendingLabel, null);
		}
	}
}
