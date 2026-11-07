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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using WaveDudeNext.Model;
using WaveDudeNext.Probes;

namespace WaveDudeNext.Avalonia.Controls;

public sealed partial class SceneCanvas
{
    public event Action<Probe>? ProbeViewportRequested;
    private Probe? _graphDrag;
    private bool _graphResize;
    private bool _graphKeyboard;
    private Point _graphStart;
    private (int X, int Y, int Width, int Height, ProbeBoxSize Size) _graphOriginal;
    private Probe? GraphAt(Point screen)
    {
        if (Project is null) return null;
        return Project.Probes.Reverse().FirstOrDefault(probe => probe.Active && probe.Layer == Layer && Overlays.ShowsGraph(probe) && GraphRect(probe).Contains(screen));
    }
    public Rect GraphRect(Probe probe)
    {
        var box = ProbeGraph.BoxInCanvas(probe, CanvasWidth, CanvasHeight);
        return new Rect(Screen(box.X, box.Y), new Size(box.Width * Mapping().Scale, box.Height * Mapping().Scale));
    }
    private bool TryBeginProbeInteraction(PointerPressedEventArgs e)
    {
        var pointer = e.GetPosition(this);
        if (GraphAt(pointer) is not { } probe) return false;
        var box = GraphRect(probe);
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            var menu = new ContextMenu();
            var detach = new MenuItem { Header = UiText.Text("Open dockable probe viewport") }; detach.Click += (_, _) => ProbeViewportRequested?.Invoke(probe); menu.Items.Add(detach);
            var hide = new MenuItem { Header = UiText.Text("Hide this graph") }; hide.Click += (_, _) => HideGraph(probe); menu.Items.Add(hide);
            var attach = new MenuItem { Header = UiText.Text("Attach graph to probe") };
            attach.Click += (_, _) => { probe.BoxX = probe.AX; probe.BoxY = probe.AY; EditCompleted?.Invoke(); InvalidateVisual(); }; menu.Items.Add(attach);
            ContextMenu = menu; menu.Open(this); e.Handled = true; return true;
        }
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return false;
        if (new Rect(box.Right - 40, box.Top, 20, 18).Contains(pointer)) { HideGraph(probe); e.Handled = true; return true; }
        Focus(); SelectionRequested?.Invoke(probe, false); SelectedObject = probe; _graphKeyboard = true;
        if (e.ClickCount == 2 || new Rect(box.Right - 20, box.Top, 20, 18).Contains(pointer))
        { ProbeViewportRequested?.Invoke(probe); e.Handled = true; return true; }
        _graphDrag = probe; _graphResize = Distance(pointer, box.BottomRight) <= 14;
        _graphStart = Canvas(pointer);
        _graphOriginal = (probe.BoxX, probe.BoxY, probe.BoxWidth, probe.BoxHeight, probe.BoxSize);
        _graphStartBox = ProbeGraph.BoxInCanvas(probe, CanvasWidth, CanvasHeight);
        _dragging = true; _manipulationPointer = e.Pointer; e.Pointer.Capture(this); e.Handled = true; InvalidateVisual(); return true;
    }
    private BoxRect _graphStartBox;
    private void MoveProbeGraph(PointerEventArgs e)
    {
        var probe = _graphDrag!;
        var point = Canvas(e.GetPosition(this));
        int dx = (int)Math.Round(point.X - _graphStart.X), dy = (int)Math.Round(point.Y - _graphStart.Y);
        if (_graphResize) ResizeProbeGraph(probe, _graphStartBox.Width + dx, _graphStartBox.Height + dy, true);
        else MoveProbeGraph(probe, _graphOriginal.X + dx, _graphOriginal.Y + dy, true);
        InvalidateVisual();
    }
    public void MoveProbeGraph(Probe probe, int x, int y, bool snap)
    {
        double dx = x - probe.AX, dy = y - probe.AY;
        if (snap && Math.Sqrt(dx * dx + dy * dy) * Mapping().Scale < (probe.IsBoxAttached ? 28 : 14))
        { probe.BoxX = probe.AX; probe.BoxY = probe.AY; return; }
        var box = ProbeGraph.BoxInCanvas(probe, CanvasWidth, CanvasHeight);
        probe.BoxX = Math.Clamp(x, 0, Math.Max(0, CanvasWidth - box.Width));
        probe.BoxY = Math.Clamp(y, 0, Math.Max(0, CanvasHeight - box.Height));
    }
    public static void ResizeProbeGraph(Probe probe, int width, int height, bool snap)
    {
        width = Math.Clamp(width, Probe.MinBoxWidth, Probe.MaxBoxSide); height = Math.Clamp(height, Probe.MinBoxHeight, Probe.MaxBoxSide);
        var size = snap ? ProbeGraph.Snap(width, height) : (ProbeBoxSize.Custom, new BoxSize(width, height));
        probe.BoxWidth = size.Item2.Width; probe.BoxHeight = size.Item2.Height; probe.BoxSize = size.Item1;
    }
    private bool HandleProbeKey(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _graphDrag is { } dragging)
        {
            dragging.BoxX = _graphOriginal.X; dragging.BoxY = _graphOriginal.Y;
            dragging.BoxWidth = _graphOriginal.Width; dragging.BoxHeight = _graphOriginal.Height; dragging.BoxSize = _graphOriginal.Size;
            _graphDrag = null; _dragging = false; var pointer = _manipulationPointer; _manipulationPointer = null; pointer?.Capture(null); InvalidateVisual(); e.Handled = true; return true;
        }
        if (!_graphKeyboard || SelectedObject is not Probe probe || !Overlays.ShowsGraph(probe)) return false;
        if (e.Key == Key.Enter) { ProbeViewportRequested?.Invoke(probe); e.Handled = true; return true; }
        if (e.Key == Key.Escape) { _graphKeyboard = false; e.Handled = true; return true; }
        int step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
        var delta = e.Key switch { Key.Left => (-step, 0), Key.Right => (step, 0), Key.Up => (0, -step), Key.Down => (0, step), _ => (0, 0) };
        if (delta == (0, 0)) return false;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        { var size = ProbeGraph.BoxSizeOf(probe); ResizeProbeGraph(probe, size.Width + delta.Item1, size.Height + delta.Item2, false); }
        else MoveProbeGraph(probe, probe.BoxX + delta.Item1, probe.BoxY + delta.Item2, false);
        EditCompleted?.Invoke(); InvalidateVisual(); e.Handled = true; return true;
    }
    private void HideGraph(Probe probe)
    {
        Overlays.HiddenGraphs.Add(probe); _graphKeyboard = false;
        InvalidateVisual(); OverlayVisibilityChanged?.Invoke();
    }
    private bool TryObjectVisibilityMenu(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed || Hit(Canvas(e.GetPosition(this))) is not { } item) return false;
        var hide = new MenuItem { Header = UiText.Text("Hide this drawing") };
        hide.Click += (_, _) => { Overlays.HiddenObjects.Add(item); InvalidateVisual(); OverlayVisibilityChanged?.Invoke(); };
        ContextMenu = new ContextMenu { Items = { hide } }; ContextMenu.Open(this); e.Handled = true; return true;
    }
}
