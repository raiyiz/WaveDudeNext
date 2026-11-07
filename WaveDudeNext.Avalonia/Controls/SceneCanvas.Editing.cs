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
using Avalonia.Input;
using Avalonia.Media;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Controls;

public sealed partial class SceneCanvas
{
    public double Zoom { get; private set; } = 1;
    public Vector Pan { get; private set; }
    public event Action? ViewChanged;
    private bool _panning;
    private Point _panStart;
    private sealed record EditHandle(double X, double Y, Action<double, double> Set, bool World = false, string Name = "Geometry handle");
    private EditHandle? _editHandle;
    public Point CanvasPoint(Point point) => Canvas(point);
    public void ResetView() { Zoom = 1; Pan = default; ViewChanged?.Invoke(); InvalidateVisual(); }
    // Same transform as the uniformly fitted bitmap beneath the overlay.
    public Matrix ImageTransform => Matrix.CreateScale(Zoom, Zoom) * Matrix.CreateTranslation(Pan);
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Point pointer = e.GetPosition(this), canvas = Canvas(pointer);
        Zoom = Math.Clamp(Zoom * Math.Pow(1.15, e.Delta.Y), 0.25, 16);
        Point after = Screen(canvas.X, canvas.Y);
        Pan += pointer - after;
        ViewChanged?.Invoke(); InvalidateVisual(); e.Handled = true;
    }
    private IEnumerable<EditHandle> EditHandles(OpticalPart part)
    {
        if (part is LensPart lens)
        {
            yield return new(lens.Thickness / 2.0, lens.Diameter / 2.0,
                (x, y) => lens.SetExtents(Math.Max(2, (int)Math.Round(2 * Math.Abs(y))), Math.Max(1, (int)Math.Round(2 * Math.Abs(x)))), Name: "Lens size");
            for (int i = 1; i <= 2; i++)
            {
                int surface = i;
                var rim = lens.ToCanvas(lens.RimX(surface), 0);
                var other = lens.ToCanvas(lens.VertexX(surface == 1 ? 2 : 1), 0);
                yield return new(lens.VertexX(surface), 0, (x, y) => lens.SetVertex(surface, x,
                    lens.ToFrame(rim.X, rim.Y).X, lens.ToFrame(other.X, other.Y).X), Name: "Lens surface " + i);
            }
            yield break;
        }
        if (part is RectanglePart rectangle)
        {
            yield return new(rectangle.Length / 2.0, rectangle.Width / 2.0, (x, y) =>
            { int a = Math.Max(1, (int)Math.Round(2 * Math.Abs(x))), b = Math.Max(1, (int)Math.Round(2 * Math.Abs(y))); rectangle.Length = rectangle.Linked ? Math.Max(a, b) : a; rectangle.Width = rectangle.Linked ? Math.Max(a, b) : b; });
            yield break;
        }
        if (part is EllipsePart ellipse)
        {
            yield return new(ellipse.Length / 2.0, ellipse.Width / 2.0, (x, y) =>
            { int a = Math.Max(1, (int)Math.Round(2 * Math.Abs(x))), b = Math.Max(1, (int)Math.Round(2 * Math.Abs(y))); ellipse.Length = ellipse.Linked ? Math.Max(a, b) : a; ellipse.Width = ellipse.Linked ? Math.Max(a, b) : b; });
            yield break;
        }
        if (part is PolygonPart polygon)
        {
            yield return new(polygon.Circumradius, 0, (x, y) => polygon.Side = Math.Max(2, (int)Math.Round(2 * Math.Sqrt(x * x + y * y) * Math.Sin(Math.PI / polygon.Sides))));
            yield break;
        }
        if (part is OutlinePart outline)
        {
            foreach (var point in outline.Points)
                yield return new(point.X, point.Y, (x, y) => { point.X = (int)Math.Round(x); point.Y = (int)Math.Round(y); });
            yield break;
        }
        if (part is FiberPart fiber)
        {
            foreach (var node in fiber.Nodes)
            {
                yield return new(node.X, node.Y, (x, y) => { node.X = (int)Math.Round(x); node.Y = (int)Math.Round(y); });
                yield return new(node.In.X, node.In.Y, (x, y) => { node.SetHandle(false, (int)Math.Round(x - node.X), (int)Math.Round(y - node.Y), null); });
                yield return new(node.Out.X, node.Out.Y, (x, y) => { node.SetHandle(true, (int)Math.Round(x - node.X), (int)Math.Round(y - node.Y), null); });
            }
            yield break;
        }
        // Axis-aligned dimensions are in the part's own rotated coordinate system.
        foreach (string name in new[] { "Length", "Thickness", "Diameter", "Width", "Base" })
        {
            var property = part.GetType().GetProperty(name);
            if (property?.CanWrite != true || property.PropertyType != typeof(int)) continue;
            int value = (int)property.GetValue(part)!;
            bool horizontal = name is "Length" or "Thickness";
            yield return new(horizontal ? value / 2.0 : 0, horizontal ? 0 : value / 2.0,
                (x, y) => property.SetValue(part, Math.Clamp((int)Math.Round(2 * Math.Abs(horizontal ? x : y)), 2, 32768)));
        }
    }
    private void DrawEditHandles(DrawingContext context, OpticalPart part)
    {
        if (part is FiberPart fiber)
            foreach (var node in fiber.Nodes)
            {
                var c = part.ToCanvas(node.X, node.Y); var a = part.ToCanvas(node.In.X, node.In.Y); var b = part.ToCanvas(node.Out.X, node.Out.Y);
                context.DrawLine(new Pen(Brushes.Gray), Screen(a.X, a.Y), Screen(c.X, c.Y));
                context.DrawLine(new Pen(Brushes.Gray), Screen(c.X, c.Y), Screen(b.X, b.Y));
            }
        foreach (var handle in EditHandles(part))
        {
            var point = part.ToCanvas(handle.X, handle.Y);
            context.DrawRectangle(Brushes.Black, new Pen(Brushes.DeepSkyBlue), new Rect(Screen(point.X, point.Y) - new Vector(4, 4), new Size(8, 8)));
        }
    }
    private EditHandle? FindEditHandle(Point pointer)
    {
        if (!Overlays.ShowsObject(SelectedObject)) return null;
        if (SelectedObject is Probe)
            foreach (var handle in KeyboardHandles())
                if (Distance(Screen(handle.X, handle.Y), pointer) <= 8) return handle;
        if (SelectedObject is Source source)
            foreach (var handle in SourceHandles(source))
                if (Distance(Screen(handle.X, handle.Y), pointer) <= 8) return handle;
        if (SelectedObject is OpticalPart part)
            foreach (var handle in EditHandles(part))
            {
                var point = part.ToCanvas(handle.X, handle.Y);
                if (Distance(Screen(point.X, point.Y), pointer) <= 7) return handle;
            }
        return null;
    }
    private IEnumerable<EditHandle> SourceHandles(Source source)
    {
        if (source.Array is not { } array) yield break;
        Point World(double x, double y)
        {
            double angle = array.Angle * Math.PI / 180;
            return new Point(source.X + x * Math.Cos(angle) + y * Math.Sin(angle), source.Y - x * Math.Sin(angle) + y * Math.Cos(angle));
        }
        Point Local(double x, double y)
        {
            double angle = array.Angle * Math.PI / 180, dx = x - source.X, dy = y - source.Y;
            return new Point(dx * Math.Cos(angle) - dy * Math.Sin(angle), dx * Math.Sin(angle) + dy * Math.Cos(angle));
        }
        double halfX = (array.SizeX - 1) * array.SpacingX / 2.0, halfY = (array.SizeY - 1) * array.SpacingY / 2.0;
        var anglePoint = World(Math.Max(halfX + 20 / Mapping().Scale, 35 / Mapping().Scale), 0);
        yield return new(anglePoint.X, anglePoint.Y, (x, y) => array.Angle = Math.Round(Math.Atan2(source.Y - y, x - source.X) * 180 / Math.PI), true, "Source rotation");
        if (array.SizeX > 1)
        {
            var point = World(halfX, 0);
            yield return new(point.X, point.Y, (x, y) => array.SpacingX = Math.Max(1, (int)Math.Round(2 * Math.Abs(Local(x, y).X) / (array.SizeX - 1))), true, "Column spacing");
        }
        if (array.SizeY > 1)
        {
            var point = World(0, halfY);
            yield return new(point.X, point.Y, (x, y) => array.SpacingY = Math.Max(1, (int)Math.Round(2 * Math.Abs(Local(x, y).Y) / (array.SizeY - 1))), true, "Row spacing");
        }
    }
    private void DrawSourceHandles(DrawingContext context, Source source)
    {
        foreach (var handle in SourceHandles(source))
        {
            var position = Screen(handle.X, handle.Y);
            context.DrawLine(new Pen(Brushes.DeepSkyBlue), Screen(source.X, source.Y), position);
            context.DrawEllipse(Brushes.Black, new Pen(Brushes.DeepSkyBlue), position, 5, 5);
        }
    }
}
