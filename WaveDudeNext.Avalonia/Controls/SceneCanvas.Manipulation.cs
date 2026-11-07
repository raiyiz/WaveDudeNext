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
using System.Linq;
using System.Reflection;
using Avalonia.Input;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Controls;

public sealed partial class SceneCanvas
{
    private readonly List<(object Model, PropertyInfo Property, object? Value)> _dragSnapshot = new();
    private IPointer? _manipulationPointer;
    private void BeginManipulation(IPointer pointer)
    {
        _dragSnapshot.Clear();
        foreach (object model in Selection.Concat(SelectedObject is null ? Array.Empty<object>() : new[] { SelectedObject }).Distinct())
        {
            Remember(model);
            if (model is Source { Array: { } array }) Remember(array);
            if (model is OutlinePart outline) foreach (var point in outline.Points) Remember(point);
            if (model is FiberPart fiber) foreach (var node in fiber.Nodes) Remember(node);
        }
        _manipulationPointer = pointer; pointer.Capture(this);
    }
    private void Remember(object model)
    {
        foreach (var property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0 &&
                (property.PropertyType.IsValueType || property.PropertyType == typeof(string)))
                _dragSnapshot.Add((model, property, property.GetValue(model)));
    }
    private bool CancelManipulation(KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !_dragging || _graphDrag is not null || _dragSnapshot.Count == 0) return false;
        _dragging = false; _editHandle = null; _rotating = false;
        // A second pass restores coupled values such as linked extents and fitted lens radii.
        foreach (var pass in new[] { 0, 1 }) foreach (var saved in _dragSnapshot) saved.Property.SetValue(saved.Model, saved.Value);
        foreach (var source in Selection.OfType<Source>()) if (Project is { } project) source.GenerateElements(project.Seed);
        _dragSnapshot.Clear(); var pointer = _manipulationPointer; _manipulationPointer = null; pointer?.Capture(null);
        InvalidateVisual(); e.Handled = true; return true;
    }
}
