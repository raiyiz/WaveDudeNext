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
using Avalonia.Controls;
using Avalonia.Interactivity;
using WaveDudeNext.Avalonia.Docking;
using WaveDudeNext.Avalonia.Controls;

namespace WaveDudeNext.Avalonia;
public partial class MainWindow
{
    private DockWorkspace _dock = null!;
    private bool _closingWorkspace;
    private bool _dockReady;
    private DockLayout? _pendingProbeLayout;
    private readonly Dictionary<WaveDudeNext.Model.Probe, int> _probeSlots = new();
    private int _nextProbeSlot;
    private readonly Dictionary<int, ProbeViewport> _probeViewports = new();
    private void InitializeDocking()
    {
        // Keep the named preview/editor controls alive while replacing their fixed grid layout.
        PreviewGrid.Children.Remove(FieldHost); PreviewGrid.Children.Remove(IntensityHost);
        WorkspaceGrid.Children.Clear(); WorkspaceGrid.ColumnDefinitions.Clear();
        InstallOverlayBar(FieldHost, FieldCanvas, _desktop.FieldOverlays, "Field");
        InstallOverlayBar(IntensityHost, IntensityCanvas, _desktop.IntensityOverlays, "Intensity");
        RemoveOldTitle(PropertiesHost.Child as Grid);
        RemoveOldTitle(ProjectTreeControl.Content as Grid);
        _pendingProbeLayout = _desktop.DockLayout;
        _nextProbeSlot = _desktop.ProbePaneIds.Values.DefaultIfEmpty(-1).Max() + 1;
        _dock = new DockWorkspace(this);
        _dock.SetScale(_desktop.Zoom);
        _dock.Register("Tree", UiText.Text("Project"), TreeHost); _dock.Register("Field", UiText.Text("Field"), FieldHost);
        _dock.Register("Intensity", UiText.Text("Intensity"), IntensityHost); _dock.Register("Properties", UiText.Text("Properties"), PropertiesHost);
        WorkspaceGrid.Children.Add(_dock);
        _dock.Reset(_desktop.TabbedPreviews);
        _dock.LayoutChanged += () => { if (_dockReady && !_closingWorkspace) { _pendingProbeLayout = null; _desktop.DockLayout = _dock.CaptureLayout(); SaveDesktop(); UpdateTimelineControls(); } };
        _dock.DragStatusChanged += text => { if (text.Length > 0) StatusText.Text = text; };
        foreach (var canvas in new[] { FieldCanvas, IntensityCanvas })
            canvas.ProbeViewportRequested += probe => OpenProbeViewport(probe);
    }
    private static void RemoveOldTitle(Grid? grid)
    {
        if (grid is null || grid.RowDefinitions.Count < 2) return;
        var header = grid.Children.FirstOrDefault(child => Grid.GetRow(child) == 0);
        if (header is not null) grid.Children.Remove(header);
        foreach (var child in grid.Children) Grid.SetRow(child, Math.Max(0, Grid.GetRow(child) - 1));
        grid.RowDefinitions.RemoveAt(0);
    }
    private void SynchronizeProbeViewports()
    {
        if (_dock is null || _loadedProject is not { } project) return;
        bool menuChanged = false;
        var activeSlots = new HashSet<int>();
        var occurrences = new Dictionary<string, int>();
        for (int i = 0; i < project.Probes.Count; i++)
        {
            var probe = project.Probes[i];
            occurrences.TryGetValue(probe.Name, out int occurrence); occurrences[probe.Name] = occurrence + 1;
            string identity = probe.Name + "\u001f" + occurrence;
            if (!_probeSlots.TryGetValue(probe, out int slot))
            {
                slot = _desktop.ProbePaneIds.TryGetValue(identity, out int previous) && !activeSlots.Contains(previous) ? previous : _nextProbeSlot++;
                _probeSlots[probe] = slot;
            }
            _desktop.ProbePaneIds[identity] = slot;
            activeSlots.Add(slot);
            if (!_probeViewports.TryGetValue(slot, out var view))
            {
                view = new ProbeViewport();
                view.SelectionRequested += selected => { ProjectTreeControl.SelectModel(selected); SelectObject(selected); };
                view.SeekRequested += delta => { _timer.Stop(); StopPlayback(); _followSimulation = false; ShowStoredFrame(_viewFrame + delta); };
                view.ShowOverlayRequested += ShowProbeOverSimulation;
                view.ExportRequested += () => ExportProbe_Click(this, new());
                _probeViewports[slot] = view; menuChanged = true;
            }
            if (!ReferenceEquals(view.Probe, probe) || view.Title != probe.Name) menuChanged = true;
            if (!ReferenceEquals(view.Probe, probe) && _dock.IsPaneVisible("Probe." + slot))
            { FieldCanvas.Overlays.HiddenGraphs.Add(probe); IntensityCanvas.Overlays.HiddenGraphs.Add(probe); }
            view.Probe = probe; view.Title = probe.Name;
            view.Trace = _probeTraces.TryGetValue(probe, out var trace) ? trace : null;
            view.Step = FieldCanvas.Step; view.Frame = FieldCanvas.Frame * FieldCanvas.ProfileFrameStride;
            view.TotalSteps = _runSteps;
            _dock.Register("Probe." + slot, probe.Name, view);
            view.InvalidateVisual();
        }
        foreach (int slot in _probeViewports.Keys.Where(slot => !activeSlots.Contains(slot)).ToArray())
        { _dock.Remove("Probe." + slot); _probeViewports.Remove(slot); menuChanged = true; }
        foreach (var old in _probeSlots.Keys.Where(probe => !project.Probes.Contains(probe)).ToArray()) _probeSlots.Remove(old);
        if (!_dockReady)
        {
            if (_desktop.DockLayout is { } layout) { _dock.Restore(layout); HideRestoredProbeOverlays(); }
            else
                foreach (var pair in _desktop.Panes.Where(p => p.Value.Floating).ToArray())
                    _dock.Float(pair.Key, new global::Avalonia.PixelPoint(pair.Value.X, pair.Value.Y));
            _dockReady = true;
        }
        if (_pendingProbeLayout is { } pending && project.Probes.Count > 0 && ProbePaneIds(pending).All(_dock.Contains))
        { _dock.Restore(pending); HideRestoredProbeOverlays(); _pendingProbeLayout = null; _desktop.DockLayout = _dock.CaptureLayout(); SaveDesktop(); }
        if (menuChanged)
        {
            ProbePanesMenu.Items.Clear();
            for (int i = 0; i < project.Probes.Count; i++)
            {
                var probe = project.Probes[i];
                var item = new MenuItem { Header = project.Probes[i].Name, Icon = new IconView { Drawing = "Probe" } };
                item.Click += (_, _) => OpenProbeViewport(probe);
                ProbePanesMenu.Items.Add(item);
            }
        }
        ProbePanesMenu.IsEnabled = project.Probes.Count > 0;
    }
    private void HideRestoredProbeOverlays()
    {
        foreach (var pair in _probeViewports)
            if (_dock.IsPaneVisible("Probe." + pair.Key) && pair.Value.Probe is { } probe)
            { FieldCanvas.Overlays.HiddenGraphs.Add(probe); IntensityCanvas.Overlays.HiddenGraphs.Add(probe); }
    }
    private static IEnumerable<string> ProbePaneIds(DockLayout layout)
    {
        IEnumerable<string> NodeIds(DockNode? node) => node is null ? Array.Empty<string>() : node.IsSplit ? NodeIds(node.First).Concat(NodeIds(node.Second)) : node.Items;
        return NodeIds(layout.Root).Concat(layout.Floating.SelectMany(f => NodeIds(f.Root))).Where(id => id.StartsWith("Probe.", StringComparison.Ordinal));
    }
    private void OpenProbeViewport(WaveDudeNext.Model.Probe probe)
    {
        SynchronizeProbeViewports();
        if (!_probeSlots.TryGetValue(probe, out int slot)) return;
        string id = "Probe." + slot;
        if (_dock.IsPaneVisible(id)) _dock.Show(id);
        else
        {
            string? other = _probeViewports.Keys.Select(key => "Probe." + key).FirstOrDefault(key => key != id && _dock.IsPaneVisible(key));
            _dock.Dock(id, other ?? "Intensity", other is null ? DockSide.Bottom : DockSide.Tab);
        }
        HideProbeOverSimulation(probe);
    }
    private void FloatPane_Click(object? sender, RoutedEventArgs e)
    { if ((sender as Control)?.Tag is string id) _dock.Float(id); }
    private void ShowPane_Click(object? sender, RoutedEventArgs e)
    { if ((sender as Control)?.Tag is string id) _dock.Show(id); }
    private void DockAll_Click(object? sender, RoutedEventArgs e) => _dock.DockAll();
    private void TabbedPreviews_Click(object? sender, RoutedEventArgs e)
    { _dock.Show("Field"); _dock.Dock("Intensity", "Field", DockSide.Tab); _desktop.TabbedPreviews = true; }
    private void SideBySide_Click(object? sender, RoutedEventArgs e)
    { _desktop.PreviewStacked = false; _dock.Show("Field"); _dock.Dock("Intensity", "Field", DockSide.Right); _desktop.TabbedPreviews = false; UpdateTimelineControls(); }
    private void ResetLayout_Click(object? sender, RoutedEventArgs e) { _dock.Reset(); _desktop.TabbedPreviews = false; }
}
