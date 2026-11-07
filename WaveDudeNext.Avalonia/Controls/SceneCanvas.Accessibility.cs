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
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Input;
using Avalonia.Media;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Controls;

public sealed partial class SceneCanvas
{
    private int _keyboardHandle = -1;
    protected override AutomationPeer OnCreateAutomationPeer() => new ScenePeer(this);
    private sealed class ScenePeer : ControlAutomationPeer
    {
        private readonly SceneCanvas _canvas;
        public ScenePeer(SceneCanvas canvas) : base(canvas) { _canvas = canvas; }
        protected override string GetNameCore()
        {
            var handles = _canvas.KeyboardHandles();
            string handle = _canvas._keyboardHandle >= 0 && _canvas._keyboardHandle < handles.Count ? ", " + UiText.Text(handles[_canvas._keyboardHandle].Name) : "";
            return UiText.Format("{0} preview, {1}{2}", UiText.Text(_canvas.Layer.ToString()), (_canvas.SelectedObject as WaveDudeNext.Model.INamed)?.Name, handle);
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        protected override string GetHelpTextCore() => "Tab selects geometry handles; arrow keys move the selected object or handle. Shift uses larger steps. Escape leaves handle editing. Control+Tab moves to the next pane. Probe graph: arrows move its box; Control+arrows resize; Enter opens a dockable viewport.";
    }
    private List<EditHandle> KeyboardHandles()
    {
        if (!Overlays.ShowsObject(SelectedObject)) return new();
        if (SelectedObject is Source source) return SourceHandles(source).ToList();
        if (SelectedObject is OpticalPart part) return EditHandles(part).ToList();
        if (SelectedObject is Probe probe)
        {
            var handles = new List<EditHandle> { new(probe.AX, probe.AY, (x, y) => { probe.AX = Math.Clamp((int)Math.Round(x), 0, CanvasWidth - 1); probe.AY = Math.Clamp((int)Math.Round(y), 0, CanvasHeight - 1); }, true, "Probe point A") };
            if (probe.Kind == ProbeKind.Line) handles.Add(new(probe.BX, probe.BY, (x, y) => { probe.BX = Math.Clamp((int)Math.Round(x), 0, CanvasWidth - 1); probe.BY = Math.Clamp((int)Math.Round(y), 0, CanvasHeight - 1); }, true, "Probe point B"));
            return handles;
        }
        return new();
    }
    private bool HandleGeometryKey(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _keyboardHandle >= 0) { _keyboardHandle = -1; InvalidateVisual(); e.Handled = true; return true; }
        var handles = KeyboardHandles();
        if (e.Key == Key.Tab && !e.KeyModifiers.HasFlag(KeyModifiers.Control) && handles.Count > 0)
        {
            int next = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? (_keyboardHandle < 0 ? handles.Count - 1 : _keyboardHandle - 1) : _keyboardHandle + 1;
            if (next < 0 || next >= handles.Count) { _keyboardHandle = -1; InvalidateVisual(); return false; }
            _graphKeyboard = false; _keyboardHandle = next; InvalidateVisual(); e.Handled = true; return true;
        }
        if (_keyboardHandle < 0 || _keyboardHandle >= handles.Count) return false;
        int step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
        var delta = e.Key switch { Key.Left => (-step, 0), Key.Right => (step, 0), Key.Up => (0, -step), Key.Down => (0, step), _ => (0, 0) };
        if (delta == (0, 0)) return false;
        var handle = handles[_keyboardHandle];
        double x = handle.X, y = handle.Y;
        if (!handle.World && SelectedObject is OpticalPart part)
        {
            var world = part.ToCanvas(x, y); var local = part.ToFrame(world.X + delta.Item1, world.Y + delta.Item2); x = local.X; y = local.Y;
        }
        else { x += delta.Item1; y += delta.Item2; }
        handle.Set(x, y);
        if (SelectedObject is Source source && Project is { } project) source.GenerateElements(project.Seed);
        EditCompleted?.Invoke(); InvalidateVisual(); e.Handled = true; return true;
    }
    private void DrawKeyboardHandle(DrawingContext context)
    {
        var handles = KeyboardHandles();
        if (_keyboardHandle < 0 || _keyboardHandle >= handles.Count) return;
        var handle = handles[_keyboardHandle];
        var point = !handle.World && SelectedObject is OpticalPart part ? part.ToCanvas(handle.X, handle.Y) : (handle.X, handle.Y);
        context.DrawEllipse(null, new Pen(Brushes.White, 2), Screen(point.Item1, point.Item2), 8, 8);
    }
}
