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
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>Always reachable outside the bitmap, even when every drawing is hidden.</summary>
public sealed class PreviewOverlayBar : UserControl
{
    private readonly SceneCanvas _canvas;
    private readonly Func<Project?> _project;
    private readonly Action _changed;
    private readonly ToggleButton _drawings;
    private readonly Button _options;
    private bool _synchronizing;
    private readonly Flyout _flyout = new();
    public PreviewOverlayBar(SceneCanvas canvas, Func<Project?> project, Action changed, string id)
    {
        Background = Application.Current?.TryFindResource("WorkspaceHeader", out var brush) == true && brush is IBrush color ? color : Brushes.DimGray;
        _canvas = canvas; _project = project; _changed = changed;
        _drawings = new ToggleButton { Content = UiText.Text("Drawings"), IsChecked = canvas.Overlays.Drawings, Padding = new Thickness(8, 2) };
        AutomationProperties.SetAutomationId(_drawings, id + "DrawingsToggle");
        AutomationProperties.SetName(_drawings, UiText.Text("Show preview drawings"));
        ToolTip.SetTip(_drawings, UiText.Text("Hide or show every line, label, marker and graph over this simulation."));
        _drawings.IsCheckedChanged += (_, _) => { if (_synchronizing) return; canvas.Overlays.Drawings = _drawings.IsChecked == true; _changed(); };
        _options = new Button { Content = UiText.Text("Overlay options…"), Padding = new Thickness(8, 2) };
        AutomationProperties.SetAutomationId(_options, id + "OverlayOptions");
        _options.Flyout = _flyout;
        _flyout.Opening += (_, _) => BuildOptions();
        Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(4, 3), Children = { _drawings, _options } };
    }
    public void SynchronizeState()
    { _synchronizing = true; try { _drawings.IsChecked = _canvas.Overlays.Drawings; } finally { _synchronizing = false; } }
    public void RefreshLanguage()
    { _drawings.Content = UiText.Text("Drawings"); _options.Content = UiText.Text("Overlay options…"); AutomationProperties.SetName(_drawings, UiText.Text("Show preview drawings")); ToolTip.SetTip(_drawings, UiText.Text("Hide or show every line, label, marker and graph over this simulation.")); }
    private void BuildOptions()
    {
        var state = _canvas.Overlays;
        var panel = new StackPanel { Spacing = 5, Width = 300 };
        void Choice(string title, bool enabled, Action<bool> update, string? id = null)
        {
            var check = new CheckBox { Content = title, IsChecked = enabled, MinHeight = 26 };
            if (id is not null) AutomationProperties.SetAutomationId(check, id);
            check.IsCheckedChanged += (_, _) => { update(check.IsChecked == true); _changed(); };
            panel.Children.Add(check);
        }
        Choice(UiText.Text("Source markers"), state.Sources, value => state.Sources = value);
        Choice(UiText.Text("Part outlines and handles"), state.Parts, value => state.Parts = value);
        Choice(UiText.Text("Probe markers"), state.ProbeMarkers, value => state.ProbeMarkers = value);
        Choice(UiText.Text("Object labels"), state.Labels, value => state.Labels = value);
        Choice(UiText.Text("Probe graphs on simulation"), state.ProbeGraphs, value => state.ProbeGraphs = value);
        Choice(UiText.Text("Geometry edge glow"), state.Edges, value => state.Edges = value);
        var show = new Button { Content = UiText.Text("Show all drawings") };
        show.Click += (_, _) => { state.ShowAll(); _drawings.IsChecked = true; _changed(); BuildOptions(); };
        panel.Children.Add(show);
        if (_project() is { } project)
        {
            panel.Children.Add(new Separator());
            panel.Children.Add(new TextBlock { Text = UiText.Text("Individual drawings"), FontWeight = global::Avalonia.Media.FontWeight.SemiBold });
            foreach (var item in project.Sources.Cast<object>().Concat(project.Parts).Concat(project.Probes))
            {
                string kind = item is Source ? "Source" : item is OpticalPart ? "Part" : "Probe";
                Choice(UiText.Text(kind) + ": " + ((WaveDudeNext.Model.INamed)item).Name, !state.HiddenObjects.Contains(item),
                    visible => { if (visible) state.HiddenObjects.Remove(item); else state.HiddenObjects.Add(item); });
            }
            foreach (var probe in project.Probes.Where(probe => probe.Layer == _canvas.Layer))
                Choice(UiText.Text("Graph") + ": " + probe.Name, !state.HiddenGraphs.Contains(probe),
                    visible => { if (visible) state.HiddenGraphs.Remove(probe); else state.HiddenGraphs.Add(probe); }, "GraphVisibility:" + probe.Name);
        }
        _flyout.Content = new ScrollViewer { Content = panel, MaxHeight = 500 };
    }
}
