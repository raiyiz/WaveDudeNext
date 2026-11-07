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
using System.Runtime.InteropServices;
using SkiaSharp;
using WaveDudeNext.Export;
using WaveDudeNext.Model;
using WaveDudeNext.Probes;

namespace WaveDudeNext.Avalonia.Rendering;

internal static class ExportDecoration
{
    public static void Draw(Project project, IReadOnlyDictionary<Probe, ProbeTrace> traces, ProbeLayer layer,
        int width, int height, int frame, int step, int totalSteps, Memory<int> pixels)
    {
        var probes = project.Probes;
        if (project.Display.ProbeMarkersInExport)
            foreach (var probe in probes)
                if (probe.Active && probe.Layer == layer) ProbeMarkers.DrawStrokes(pixels.Span, width, height, probe);
        if (!project.Display.ProbeGraphsInExport && !project.Display.ProbeMarkersInExport) return;
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque));
        var buffer = pixels.ToArray();
        Marshal.Copy(buffer, 0, bitmap.GetPixels(), buffer.Length);
        using var canvas = new SKCanvas(bitmap);
        using var font = new SKFont(SKTypeface.Default, 11);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        void Text(string text, float x, float y) { paint.Style = SKPaintStyle.Fill; paint.Color = SKColors.White; canvas.DrawText(text, x, y + font.Size, SKTextAlign.Left, font, paint); }
        int index = 0;
        foreach (var probe in probes)
        {
            index++;
            if (!probe.Active || probe.Layer != layer) continue;
            if (project.Display.ProbeMarkersInExport)
            {
                var a = ProbeMarkers.LabelOrigin(probe.AX, probe.AY, height);
                Text(index + (probe.Kind == ProbeKind.Line ? "A" : ""), a.X, a.Y);
                if (probe.Kind == ProbeKind.Line) { var b = ProbeMarkers.LabelOrigin(probe.BX, probe.BY, height); Text(index + "B", b.X, b.Y); }
            }
            if (!project.Display.ProbeGraphsInExport) continue;
            var box = ProbeGraph.BoxInCanvas(probe, width, height);
            var data = traces.TryGetValue(probe, out var trace) ? ProbeGraphData.At(trace, step, frame) : default;
            var style = new ProbeGraphStyle(11, 14, text => font.MeasureText(text));
            var graph = ProbeGraph.Layout(probe, data, totalSteps, (float)box.Width, (float)box.Height, style, CultureInfo.CurrentCulture);
            if (!graph.IsVisible) continue;
            canvas.Save(); canvas.Translate((float)box.X, (float)box.Y); canvas.ClipRect(SKRect.Create((float)box.Width, (float)box.Height));
            paint.Style = SKPaintStyle.Fill; paint.Color = new SKColor(20, 20, 20, 220); canvas.DrawRect(SKRect.Create((float)box.Width, (float)box.Height), paint);
            Text(graph.Title, 3, 2);
            if (graph.HasPlot)
            {
                foreach (var tick in graph.YTicks)
                {
                    paint.Color = SKColors.Gray; paint.StrokeWidth = 1; canvas.DrawLine(graph.PlotX, tick.Position, graph.PlotX + graph.PlotWidth, tick.Position, paint);
                    if (graph.ShowLabels) Text(tick.Label, 2, tick.Position - 6);
                }
                foreach (var tick in graph.XTicks)
                {
                    paint.Color = SKColors.Gray; canvas.DrawLine(tick.Position, graph.PlotY, tick.Position, graph.PlotY + graph.PlotHeight, paint);
                    if (graph.ShowLabels) Text(tick.Label, tick.Position, graph.PlotY + graph.PlotHeight + 2);
                }
                canvas.ClipRect(SKRect.Create(graph.PlotX, graph.PlotY, graph.PlotWidth, graph.PlotHeight));
                paint.Color = SKColors.LimeGreen; paint.StrokeWidth = graph.LineWidth;
                var curve = graph.Curve;
                for (int i = 1; i < curve.Length; i++) canvas.DrawLine(curve[i - 1].X, curve[i - 1].Y, curve[i].X, curve[i].Y, paint);
            }
            canvas.Restore();
        }
        canvas.Flush();
        Marshal.Copy(bitmap.GetPixels(), buffer, 0, buffer.Length);
        buffer.AsSpan().CopyTo(pixels.Span);
    }
}
