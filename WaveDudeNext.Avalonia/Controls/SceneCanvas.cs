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
public sealed partial class SceneCanvas : Control
{
    public PreviewOverlays Overlays { get; set; } = new();
    public event Action? OverlayVisibilityChanged;
    private Project? _project;
    public Project? Project
    {
        get => _project;
        set
        {
            if (!ReferenceEquals(value, _project)) { Overlays.HiddenObjects.Clear(); Overlays.HiddenGraphs.Clear(); }
            _project = value;
        }
    }
    private object? _selectedModel;
    public object? SelectedObject { get => _selectedModel; set { if (!ReferenceEquals(value, _selectedModel)) _keyboardHandle = -1; _selectedModel = value; } }
    public IReadOnlyList<object> Selection { get; set; } = Array.Empty<object>();
    public IReadOnlyDictionary<Probe, ProbeTrace>? Traces { get; set; }
    public ProbeLayer Layer { get; set; }
    public int Frame { get; set; }
    public int ProfileFrameStride { get; set; } = 1;
    public int Step { get; set; }
    public int TotalSteps { get; set; }
    public int CanvasWidth { get; set; } = 256;
    public int CanvasHeight { get; set; } = 256;
    public event Action<object?, bool>? SelectionRequested;
    public event Action? EditCompleted;
    private Point _last;
    private bool _dragging;
    private bool _rotating;
    public bool IsManipulating => _dragging;
    public SceneCanvas() { Focusable = true; ClipToBounds = true; }
    private (double Scale, Point Origin) Mapping()
    {
        double scale = Math.Min(Bounds.Width / CanvasWidth, Bounds.Height / CanvasHeight) * Zoom;
        return (Math.Max(scale, 0.000001), new Point((Bounds.Width - CanvasWidth * scale) / 2 + Pan.X, (Bounds.Height - CanvasHeight * scale) / 2 + Pan.Y));
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
        if (Project is not { } project || !Overlays.Drawings) return;
        bool handles = project.Display.HandlesInPreview;
        foreach (var part in project.Parts)
        {
            if (!Overlays.ShowsObject(part)) continue;
            bool selected = Selection.Contains(part);
            if (!handles && !selected) continue;
            var points = part.Outline().Select(p => part.ToCanvas(p.X, p.Y)).Select(p => Screen(p.X, p.Y)).ToArray();
            var pen = new Pen(selected ? Brushes.DeepSkyBlue : Brushes.Gray, selected ? 2 : 1);
            for (int i = 1; i < points.Length; i++) context.DrawLine(pen, points[i - 1], points[i]);
            if (points.Length > 2 && part.OutlineClosed) context.DrawLine(pen, points[^1], points[0]);
            Point center = Screen(part.X, part.Y);
            context.DrawEllipse(selected ? Brushes.DeepSkyBlue : Brushes.Gray, null, center, 3, 3);
            if (selected)
            {
                DrawEditHandles(context, part);
                var rotation = part.ToCanvas(part.AxisReach + 20, 0);
                Point end = Screen(rotation.X, rotation.Y);
                context.DrawLine(pen, center, end);
                context.DrawEllipse(Brushes.Black, pen, end, 5, 5);
            }
            if (Overlays.Labels && (project.Display.LabelsInPreview || selected)) Label(context, part.Name, center);
        }
        foreach (var source in project.Sources)
        {
            if (!Overlays.ShowsObject(source)) continue;
            bool selected = Selection.Contains(source);
            if (!handles && !selected) continue;
            Point point = Screen(source.X, source.Y);
            var pen = new Pen(selected ? Brushes.DeepSkyBlue : Brushes.Yellow, 1.5);
            context.DrawEllipse(null, pen, point, 5, 5);
            context.DrawLine(pen, point + new Vector(-8, 0), point + new Vector(8, 0));
            context.DrawLine(pen, point + new Vector(0, -8), point + new Vector(0, 8));
            if (selected) DrawSourceHandles(context, source);
            if (Overlays.Labels && (project.Display.LabelsInPreview || selected)) Label(context, source.Name, point);
        }
        foreach (var probe in project.Probes)
        {
            if (probe.Layer != Layer || !probe.Active) continue;
            if (Overlays.ShowsGraph(probe)) DrawGraph(context, probe);
            if (!Overlays.ShowsObject(probe)) continue;
            bool selected = Selection.Contains(probe);
            if (!handles && !selected) continue;
            Point point = Screen(probe.AX, probe.AY);
            var pen = new Pen(selected ? Brushes.DeepSkyBlue : Brushes.LimeGreen, 1.5);
            context.DrawEllipse(null, pen, point, 4, 4);
            if (probe.Kind == ProbeKind.Line) { var end = Screen(probe.BX, probe.BY); context.DrawLine(pen, point, end); context.DrawEllipse(null, pen, end, 5, 5); }
            if (Overlays.Labels && (project.Display.LabelsInPreview || selected)) Label(context, probe.Name, point);
        }
        if (Overlays.ShowsObject(SelectedObject)) DrawKeyboardHandle(context);
    }
    private void DrawGraph(DrawingContext context, Probe probe)
    {
        var box = ProbeGraph.BoxInCanvas(probe, CanvasWidth, CanvasHeight);
        var data = Traces is not null && Traces.TryGetValue(probe, out var trace)
            ? ProbeGraphData.At(trace, Step, Frame * ProfileFrameStride) : default;
        Point origin = Screen(box.X, box.Y);
        double scale = Mapping().Scale;
        float width = (float)(box.Width * scale), height = (float)(box.Height * scale);
        using var transform = context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y));
        ProbeViewport.Draw(context, probe, Traces is not null && Traces.TryGetValue(probe, out var recording) ? recording : null,
            Step, Frame * ProfileFrameStride, TotalSteps, width, height, 40);
        var pen = new Pen(Selection.Contains(probe) ? Brushes.DeepSkyBlue : Brushes.Gray, 1);
        context.DrawRectangle(null, pen, new Rect(0, 0, width, height));
        context.DrawRectangle(Brushes.Black, pen, new Rect(Math.Max(0, width - 8), Math.Max(0, height - 8), 8, 8));
        context.DrawRectangle(null, pen, new Rect(Math.Max(0, width - 17), 3, 11, 10));
        context.DrawLine(pen, new Point(width - 34, 4), new Point(width - 26, 12));
        context.DrawLine(pen, new Point(width - 34, 12), new Point(width - 26, 4));
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
        foreach (object item in Objects().Where(Overlays.ShowsObject))
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
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed) ContextMenu = null;
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        { _panning = true; _panStart = e.GetPosition(this); e.Pointer.Capture(this); e.Handled = true; return; }
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && FindEditHandle(e.GetPosition(this)) is { } detail)
        { Focus(); _graphKeyboard = false; _keyboardHandle = -1; _editHandle = detail; _dragging = true; BeginManipulation(e.Pointer); e.Handled = true; return; }
        if (TryBeginProbeInteraction(e)) return;
        if (TryObjectVisibilityMenu(e)) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _graphKeyboard = false; _keyboardHandle = -1;
        Focus();
        Point point = Canvas(e.GetPosition(this));
        _editHandle = FindEditHandle(e.GetPosition(this));
        if (_editHandle is not null) { _dragging = true; BeginManipulation(e.Pointer); e.Handled = true; return; }
        _rotating = false;
        if (SelectedObject is OpticalPart part && Overlays.ShowsObject(part))
        {
            var handle = part.ToCanvas(part.AxisReach + 20, 0);
            _rotating = Distance(e.GetPosition(this), Screen(handle.X, handle.Y)) <= 9;
        }
        object? hit = _rotating ? SelectedObject : Hit(point);
        SelectionRequested?.Invoke(hit, e.KeyModifiers.HasFlag(KeyModifiers.Control));
        SelectedObject = hit;
        _last = point;
        _dragging = hit is IMovable;
        if (_dragging) BeginManipulation(e.Pointer);
        InvalidateVisual();
        e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_graphDrag is not null) { MoveProbeGraph(e); e.Handled = true; return; }
        if (_panning) { Point now = e.GetPosition(this); Pan += now - _panStart; _panStart = now; ViewChanged?.Invoke(); InvalidateVisual(); return; }
        if (_dragging && _editHandle is { } handle)
        {
            var canvas = Canvas(e.GetPosition(this));
            var local = !handle.World && SelectedObject is OpticalPart selected ? selected.ToFrame(canvas.X, canvas.Y) : (canvas.X, canvas.Y);
            if (SelectedObject is Source source && handle.Name == "Source rotation" && source.Array is { } array && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                array.Angle = Math.Round(Math.Atan2(source.Y - canvas.Y, canvas.X - source.X) * 180 / Math.PI / 15) * 15;
            else handle.Set(local.Item1, local.Item2);
            if (SelectedObject is Source edited && Project is { } project) edited.GenerateElements(project.Seed);
            InvalidateVisual(); return;
        }
        if (!_dragging || SelectedObject is not IMovable movable) return;
        Point point = Canvas(e.GetPosition(this));
        if (_rotating && SelectedObject is OpticalPart part)
        {
            double unit = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 15 : 1;
            part.Angle = Math.Round(Math.Atan2(part.Y - point.Y, point.X - part.X) * 180 / Math.PI / unit) * unit;
        }
        else
        {
            int dx = (int)Math.Round(point.X - _last.X), dy = (int)Math.Round(point.Y - _last.Y);
            var group = Selection.OfType<IMovable>().ToArray();
            (dx, dy) = Movable.Limit(group, dx, dy, CanvasWidth, CanvasHeight);
            foreach (var item in group) item.MoveBy(dx, dy);
            _last += new Vector(dx, dy);
        }
        InvalidateVisual();
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _panning = false; _editHandle = null; _graphDrag = null; _dragSnapshot.Clear(); _manipulationPointer = null;
        bool edited = _dragging;
        _dragging = false;
        e.Pointer.Capture(null);
        if (edited) EditCompleted?.Invoke();
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _panning = false; _editHandle = null; _graphDrag = null; _dragSnapshot.Clear(); _manipulationPointer = null;
        bool edited = _dragging;
        _dragging = false;
        if (edited) EditCompleted?.Invoke();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (CancelManipulation(e)) return;
        if (HandleGeometryKey(e)) return;
        if (HandleProbeKey(e)) return;
        if (!Overlays.ShowsObject(SelectedObject) || SelectedObject is not IMovable movable) return;
        int step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
        var delta = e.Key switch { Key.Left => (-step, 0), Key.Right => (step, 0), Key.Up => (0, -step), Key.Down => (0, step), _ => (0, 0) };
        if (delta == (0, 0)) return;
        var group = Selection.OfType<IMovable>().ToArray();
        var limited = Movable.Limit(group, delta.Item1, delta.Item2, CanvasWidth, CanvasHeight);
        foreach (var item in group) item.MoveBy(limited.Dx, limited.Dy);
        EditCompleted?.Invoke();
        e.Handled = true;
    }
}
