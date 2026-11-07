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
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace WaveDudeNext.Avalonia.Docking;

public sealed partial class DockWorkspace
{
    private string? _dragId;
    private Control? _dragHeader;
    private IPointer? _dragPointer;
    private PixelPoint _pressScreen;
    private PixelPoint _lastScreen;
    private bool _dragStarted;
    private (DockNode Node, DockSide Side)? _dropTarget;
    public bool IsDragging => _dragStarted;
    private void WireHeader(Control header, string id)
    {
        header.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(header).Properties.IsLeftButtonPressed) return;
            if (e.ClickCount == 2) { AbortDrag(); Float(id); e.Handled = true; return; }
            _dragId = id; _dragHeader = header; _dragPointer = e.Pointer;
            _pressScreen = _lastScreen = header.PointToScreen(e.GetPosition(header));
            _dragStarted = false;
            e.Pointer.Capture(header);
        };
        header.PointerMoved += (_, e) =>
        {
            if (_dragId != id || _dragHeader != header) return;
            var screen = header.PointToScreen(e.GetPosition(header));
            _lastScreen = screen;
            if (!_dragStarted && Math.Abs(screen.X - _pressScreen.X) + Math.Abs(screen.Y - _pressScreen.Y) < 8) return;
            _dragStarted = true;
            UpdateDropTarget(screen);
            e.Handled = true;
        };
        header.PointerReleased += (_, e) =>
        {
            if (_dragId != id) return;
            bool moved = _dragStarted; var target = _dropTarget; var screen = _lastScreen;
            AbortDrag();
            if (!moved) return;
            if (target is { } hit) Dock(id, hit.Node.Items.FirstOrDefault(key => key != id) ?? hit.Node.Items.FirstOrDefault(), hit.Side);
            else Float(id, new PixelPoint(screen.X - 40, screen.Y - 12));
            e.Handled = true;
        };
        header.PointerCaptureLost += (_, _) => { if (_dragHeader == header) AbortDrag(); };
    }
    private void OnDragKey(object? sender, KeyEventArgs e)
    { if (e.Key == Key.Escape && _dragId is not null) { AbortDrag(); e.Handled = true; } }
    public void CancelDrag() => AbortDrag();
    private void AbortDrag()
    {
        var pointer = _dragPointer;
        _dragId = null; _dragHeader = null; _dragPointer = null; _dragStarted = false; _dropTarget = null;
        foreach (var group in _groups) group.Hint.IsVisible = false;
        pointer?.Capture(null);
        DragStatusChanged?.Invoke("");
    }
    private void UpdateDropTarget(PixelPoint point)
    {
        _dropTarget = null;
        foreach (var group in _groups) group.Hint.IsVisible = false;
        // Reverse order gives floating surfaces priority over the main workspace.
        foreach (var group in _groups.AsEnumerable().Reverse())
        {
            if (group.Node.Items.Count == 1 && group.Node.Items[0] == _dragId) continue;
            if (!group.Grid.IsAttachedToVisualTree() || !group.Grid.IsEffectivelyVisible) continue;
            var local = group.Grid.PointToClient(point);
            if (!new Rect(group.Grid.Bounds.Size).Contains(local)) continue;
            DockSide side = SideAt(local, group.Grid.Bounds.Size);
            _dropTarget = (group.Node, side);
            var hint = group.Hint;
            hint.HorizontalAlignment = side == DockSide.Left ? HorizontalAlignment.Left : side == DockSide.Right ? HorizontalAlignment.Right : HorizontalAlignment.Stretch;
            hint.VerticalAlignment = side == DockSide.Top ? VerticalAlignment.Top : side == DockSide.Bottom ? VerticalAlignment.Bottom : VerticalAlignment.Stretch;
            hint.Width = side is DockSide.Left or DockSide.Right ? group.Grid.Bounds.Width * 0.35 : double.NaN;
            hint.Height = side is DockSide.Top or DockSide.Bottom ? group.Grid.Bounds.Height * 0.35 : double.NaN;
            hint.IsVisible = true;
            DragStatusChanged?.Invoke(side == DockSide.Tab ? UiText.Text("Release to dock as a tab. Escape cancels.") : UiText.Format("Release to dock {0}. Escape cancels.", UiText.Text(side.ToString())));
            return;
        }
        DragStatusChanged?.Invoke(UiText.Text("Release to float the pane. Escape cancels."));
    }
    public static DockSide SideAt(Point point, Size size)
    {
        double x = point.X / Math.Max(1, size.Width), y = point.Y / Math.Max(1, size.Height);
        double nearest = Math.Min(Math.Min(x, 1 - x), Math.Min(y, 1 - y));
        if (nearest > 0.24) return DockSide.Tab;
        if (nearest == x) return DockSide.Left;
        if (nearest == 1 - x) return DockSide.Right;
        return nearest == y ? DockSide.Top : DockSide.Bottom;
    }
}
