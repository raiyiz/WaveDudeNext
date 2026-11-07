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
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WaveDudeNext.Model;
using WaveDudeNext.Probes;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>Canvas selection, movement, rotation and model outlines over a uniformly scaled preview.</summary>
public sealed class SceneCanvas : Control
{
    public Project? Project { get; set; }
    public object? SelectedObject { get; set; }
    public IReadOnlyDictionary<Probe, ProbeTrace>? Traces { get; set; }
    public ProbeLayer Layer { get; set; }
    public int Frame { get; set; }
    public int Step { get; set; }
    public int TotalSteps { get; set; }
    public int CanvasWidth { get; set; } = 256;
    public int CanvasHeight { get; set; } = 256;
    public event Action<object?>? SelectionRequested;
    public event Action? EditCompleted;
    private Point _last;
    private bool _dragging;
    private bool _rotating;
    public bool IsManipulating => _dragging;
    public SceneCanvas() { Focusable = true; ClipToBounds = true; }
    private (double Scale, Point Origin) Mapping()
    {
        double scale = Math.Min(Bounds.Width / CanvasWidth, Bounds.Height / CanvasHeight);
        return (Math.Max(scale, 0.000001), new Point((Bounds.Width - CanvasWidth * scale) / 2, (Bounds.Height - CanvasHeight * scale) / 2));
    }
    private Point Screen(double x, double y)
    {
        var (scale, origin) = Mapping();
        return new Point(origin.X + x * scale, origin.Y + y * scale);
    }
    private Point Canvas(Point point)
    {
        var (scale, origin) = Mapping();
        return new Point((point.X - origin.X) / scale, (point.Y - origin.Y) / scale);
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.Transparent, Bounds.WithX(0).WithY(0));
        if (Project is not { } project) return;
        bool handles = project.Display.HandlesInPreview;
        foreach (var part in project.Parts)
        {
            bool selected = ReferenceEquals(part, SelectedObject);
            if (!handles && !selected) continue;
            var points = part.Outline().Select(p => part.ToCanvas(p.X, p.Y)).Select(p => Screen(p.X, p.Y)).ToArray();
            var pen = new Pen(selected ? Brushes.DeepSkyBlue : Brushes.Gray, selected ? 2 : 1);
            for (int i = 1; i < points.Length; i++) context.DrawLine(pen, points[i - 1], points[i]);
            if (points.Length > 2 && part.OutlineClosed) context.DrawLine(pen, points[^1], points[0]);
            Point center = Screen(part.X, part.Y);
            context.DrawEllipse(selected ? Brushes.DeepSkyBlue : Brushes.Gray, null, center, 3, 3);
            if (selected)
            {
                var rotation = part.ToCanvas(part.AxisReach + 20, 0);
                Point end = Screen(rotation.X, rotation.Y);
                context.DrawLine(pen, center, end);
                context.DrawEllipse(Brushes.Black, pen, end, 5, 5);
            }
            if (project.Display.LabelsInPreview || selected) Label(context, part.Name, center);
        }
        foreach (var source in project.Sources)
        {
            bool selected = ReferenceEquals(source, SelectedObject);
            if (!handles && !selected) continue;
            Point point = Screen(source.X, source.Y);
            var pen = new Pen(selected ? Brushes.DeepSkyBlue : Brushes.Yellow, 1.5);
            context.DrawEllipse(null, pen, point, 5, 5);
            context.DrawLine(pen, point + new Vector(-8, 0), point + new Vector(8, 0));
            context.DrawLine(pen, point + new Vector(0, -8), point + new Vector(0, 8));
            if (project.Display.LabelsInPreview || selected) Label(context, source.Name, point);
        }
        foreach (var probe in project.Probes)
        {
            if (probe.Layer != Layer || !probe.Active) continue;
            DrawGraph(context, probe);
            bool selected = ReferenceEquals(probe, SelectedObject);
            if (!handles && !selected) continue;
            Point point = Screen(probe.AX, probe.AY);
            var pen = new Pen(selected ? Brushes.DeepSkyBlue : Brushes.LimeGreen, 1.5);
            context.DrawEllipse(null, pen, point, 4, 4);
            if (probe.Kind == ProbeKind.Line) context.DrawLine(pen, point, Screen(probe.BX, probe.BY));
            if (project.Display.LabelsInPreview || selected) Label(context, probe.Name, point);
        }
    }
    private void DrawGraph(DrawingContext context, Probe probe)
    {
        var box = ProbeGraph.BoxInCanvas(probe, CanvasWidth, CanvasHeight);
        var data = Traces is not null && Traces.TryGetValue(probe, out var trace)
            ? ProbeGraphData.At(trace, Step, Frame) : default;
        Point origin = Screen(box.X, box.Y);
        double scale = Mapping().Scale;
        float width = (float)(box.Width * scale), height = (float)(box.Height * scale);
        var style = new ProbeGraphStyle(10, 13, text => (float)new FormattedText(text, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.White).Width);
        var graph = ProbeGraph.Layout(probe, data, TotalSteps, width, height, style, CultureInfo.CurrentCulture);
        if (!graph.IsVisible) return;
        using var transform = context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y));
        using var boxClip = context.PushClip(new Rect(0, 0, width, height));
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(210, 20, 20, 20)), new Pen(Brushes.Gray, 1), new Rect(0, 0, width, height));
        void Text(string value, double x, double y)
        {
            var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.White);
            context.DrawText(text, new Point(x, y));
        }
        Text(graph.Title, 3, 2);
        if (!graph.HasPlot) return;
        Rect plot = new(graph.PlotX, graph.PlotY, graph.PlotWidth, graph.PlotHeight);
        var grid = new Pen(new SolidColorBrush(Color.FromArgb(90, 160, 160, 160)), 1);
        foreach (var tick in graph.YTicks)
        {
            context.DrawLine(grid, new Point(plot.Left, tick.Position), new Point(plot.Right, tick.Position));
            if (graph.ShowLabels) Text(tick.Label, 2, tick.Position - 6);
        }
        foreach (var tick in graph.XTicks)
        {
            context.DrawLine(grid, new Point(tick.Position, plot.Top), new Point(tick.Position, plot.Bottom));
            if (graph.ShowLabels) Text(tick.Label, tick.Position, plot.Bottom + 2);
        }
        using var plotClip = context.PushClip(plot);
        var curve = graph.Curve;
        var pen = new Pen(Brushes.LimeGreen, graph.LineWidth);
        for (int i = 1; i < curve.Length; i++) context.DrawLine(pen, new Point(curve[i - 1].X, curve[i - 1].Y), new Point(curve[i].X, curve[i].Y));
    }
    private static void Label(DrawingContext context, string name, Point point) =>
        context.DrawText(new FormattedText(name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            Typeface.Default, 11, Brushes.White), point + new Vector(8, -16));
    private IEnumerable<object> Objects() => Project is not { } p ? [] :
        p.Sources.Cast<object>().Concat(p.Probes).Concat(p.Parts.Reverse());
    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    private object? Hit(Point point)
    {
        double tolerance = 10 / Mapping().Scale;
        foreach (object item in Objects())
        {
            var anchors = ((IMovable)item).Anchors;
            if (anchors.Any(p => Distance(point, new Point(p.X, p.Y)) <= tolerance)) return item;
            if (item is OpticalPart part)
            {
                var polygon = part.Outline().Select(p => part.ToCanvas(p.X, p.Y)).ToArray();
                bool inside = false;
                for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                {
                    var a = polygon[i]; var b = polygon[j];
                    if ((a.Y > point.Y) != (b.Y > point.Y)
                        && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
                }
                if (part.OutlineClosed && inside) return part;
            }
        }
        return null;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        Focus();
        Point point = Canvas(e.GetPosition(this));
        _rotating = false;
        if (SelectedObject is OpticalPart part)
        {
            var handle = part.ToCanvas(part.AxisReach + 20, 0);
            _rotating = Distance(e.GetPosition(this), Screen(handle.X, handle.Y)) <= 9;
        }
        object? hit = _rotating ? SelectedObject : Hit(point);
        SelectedObject = hit;
        SelectionRequested?.Invoke(hit);
        _last = point;
        _dragging = hit is IMovable;
        if (_dragging) e.Pointer.Capture(this);
        InvalidateVisual();
        e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || SelectedObject is not IMovable movable) return;
        Point point = Canvas(e.GetPosition(this));
        if (_rotating && SelectedObject is OpticalPart part)
            part.Angle = Math.Atan2(part.Y - point.Y, point.X - part.X) * 180 / Math.PI;
        else
        {
            int dx = (int)Math.Round(point.X - _last.X), dy = (int)Math.Round(point.Y - _last.Y);
            (dx, dy) = Movable.Limit([movable], dx, dy, CanvasWidth, CanvasHeight);
            movable.MoveBy(dx, dy);
            _last += new Vector(dx, dy);
        }
        InvalidateVisual();
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        bool edited = _dragging;
        _dragging = false;
        e.Pointer.Capture(null);
        if (edited) EditCompleted?.Invoke();
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        bool edited = _dragging;
        _dragging = false;
        if (edited) EditCompleted?.Invoke();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (SelectedObject is not IMovable movable) return;
        int step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
        var delta = e.Key switch { Key.Left => (-step, 0), Key.Right => (step, 0), Key.Up => (0, -step), Key.Down => (0, step), _ => (0, 0) };
        if (delta == (0, 0)) return;
        var limited = Movable.Limit([movable], delta.Item1, delta.Item2, CanvasWidth, CanvasHeight);
        movable.MoveBy(limited.Dx, limited.Dy);
        EditCompleted?.Invoke();
        e.Handled = true;
    }
}
