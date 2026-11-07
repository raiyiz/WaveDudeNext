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
using System.Globalization;
using Avalonia.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using WaveDudeNext.Model;
using WaveDudeNext.Probes;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>A probe graph that can live in any docked/floating pane.</summary>
public sealed class ProbeViewport : Control
{
    public event Action<Probe>? SelectionRequested;
    public event Action<int>? SeekRequested;
    public event Action? ExportRequested;
    public event Action<Probe>? ShowOverlayRequested;
    public Probe? Probe { get; set; }
    public string Title { get; set; } = "Probe";
    public ProbeTrace? Trace { get; set; }
    public int Step { get; set; }
    public int Frame { get; set; }
    public int TotalSteps { get; set; }
    public ProbeViewport()
    {
        Focusable = true; MinWidth = 100; MinHeight = 70;
        var export = new MenuItem { Header = UiText.Text("Export selected probe CSV…") };
        export.Click += (_, _) => { if (Probe is { } probe) SelectionRequested?.Invoke(probe); ExportRequested?.Invoke(); };
        var overlay = new MenuItem { Header = UiText.Text("Show graph over simulation") };
        overlay.Click += (_, _) => { if (Probe is { } probe) ShowOverlayRequested?.Invoke(probe); };
        ContextMenu = new ContextMenu { Items = { export, overlay } };
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e); Focus();
        if (Probe is { } probe) SelectionRequested?.Invoke(probe);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int delta = e.Key == Key.Left ? -1 : e.Key == Key.Right ? 1 : 0;
        if (delta != 0) { SeekRequested?.Invoke(delta * (e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1)); e.Handled = true; }
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Probe is { } probe) Draw(context, probe, Trace, Step, Frame, TotalSteps, (float)Bounds.Width, (float)Bounds.Height);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new GraphPeer(this);
    private sealed class GraphPeer : ControlAutomationPeer
    {
        private readonly ProbeViewport _view;
        public GraphPeer(ProbeViewport view) : base(view) { _view = view; }
        protected override string GetNameCore() => _view.Probe?.Name ?? _view.Title;
        protected override string GetItemStatusCore() => UiText.Format("Step {0}/{1}", _view.Step, _view.TotalSteps);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        protected override string GetHelpTextCore() => $"Probe graph, simulation step {_view.Step}. Use the project tree to edit the probe or export its data as CSV.";
    }
    public static void Draw(DrawingContext context, Probe probe, ProbeTrace? trace, int step, int frame, int totalSteps, float width, float height, float toolbarWidth = 0)
    {
        if (width < 1 || height < 1) return;
        var data = trace is not null ? ProbeGraphData.At(trace, step, frame) : default;
        var style = new ProbeGraphStyle(10, 13, text => (float)new FormattedText(text, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.White).Width);
        var graph = ProbeGraph.Layout(probe, data, totalSteps, width, height, style, CultureInfo.CurrentCulture);
        if (!graph.IsVisible) return;
        using var boxClip = context.PushClip(new Rect(0, 0, width, height));
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(210, 20, 20, 20)), new Pen(Brushes.Gray, 1), new Rect(0, 0, width, height));
        void Text(string value, double x, double y)
        {
            var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.White);
            context.DrawText(text, new Point(x, y));
        }
        Text(graph.Title, 3, 2);
        if (!string.IsNullOrEmpty(graph.Readout))
        {
            var readout = new FormattedText(graph.Readout, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.White);
            if (width - readout.Width - toolbarWidth > 70) context.DrawText(readout, new Point(width - readout.Width - 6 - toolbarWidth, 2));
        }
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
}
