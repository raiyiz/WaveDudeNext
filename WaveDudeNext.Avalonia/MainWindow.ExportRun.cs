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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using WaveDudeNext.Model;
using WaveDudeNext.Runs;
using WaveDudeNext.Rendering;
using WaveDudeNext.Engine;

namespace WaveDudeNext.Avalonia;
public partial class MainWindow
{
    private async Task<FrameStore?> PrepareExportFrames(bool intensityRoot, string title = "Preparing full-quality export")
    {
        if (_loadedProject is not { } project) return null;
        var spec = RunSpec.Create(project, Rendering.SceneImageLoader.Compose(project, PropertiesEditor.ProjectFolder), true, true,
            new PreviewQuality(1, 1, FrameFormat.Half, intensityRoot));
        var store = FrameStore.Create(Path.Combine(Path.GetTempPath(), "wavedude-export-" + Guid.NewGuid().ToString("N") + ".frames"),
            spec.Shape, spec.Layers, spec.StoredFrameCount, spec.VisualizeEvery, Math.Min(FrameMemory.Budget(10), 512L << 20));
        using var run = new SimulationRun(spec, store, SynchronizationContext.Current);
        var progress = new Window { Title = title, Width = 440, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var text = new TextBlock { Text = "Simulating at full quality…" };
        var cancel = new Button { Content = Controls.IconView.Label("Exit", "Cancel") };
        cancel.Click += (_, _) => run.Stop();
        bool finished = false;
        progress.Closing += (_, args) => { if (!finished) { args.Cancel = true; run.Stop(); } };
        progress.Content = new StackPanel { Margin = new global::Avalonia.Thickness(16), Spacing = 12, Children = { text, cancel } };
        run.PropertyChanged += (_, _) => text.Text = $"Step {run.StepsDone}/{run.TotalSteps}";
        run.Start();
        var shown = progress.ShowDialog(this);
        await run.Completion;
        finished = true; progress.Close(); await shown;
        if (run.Error is { } error) { store.Dispose(); throw error; }
        if (run.State != RunState.Completed) { store.Dispose(); return null; }
        _exportTraces.Clear();
        for (int i = 0; i < spec.Probes.Length; i++) if (spec.Probes[i].Origin is { } probe) _exportTraces[probe] = run.Traces[i];
        return store;
    }
    private async void RerunProbes_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _timer.Stop(); _editTimer.Stop(); StopPlayback();
        await _backgroundTask;
        try
        {
            using var store = await PrepareExportFrames(true, "Re-recording probes");
            if (store is null) return;
            _probeTraces.Clear();
            FieldCanvas.ProfileFrameStride = IntensityCanvas.ProfileFrameStride = _quality.FrameDivisor;
            foreach (var pair in _exportTraces) _probeTraces[pair.Key] = pair.Value;
            FieldCanvas.InvalidateVisual(); IntensityCanvas.InvalidateVisual();
            StatusText.Text = "Probe recordings updated; preview frames retained.";
        }
        catch (Exception ex) { StatusText.Text = "Probe recording failed: " + ex.Message; }
        finally { _exportTraces.Clear(); }
    }
    private readonly System.Collections.Generic.Dictionary<Probe, WaveDudeNext.Probes.ProbeTrace> _exportTraces = new();
}
