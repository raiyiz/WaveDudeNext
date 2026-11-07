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
using System.Threading.Tasks;
using WaveDudeNext.Engine;
using WaveDudeNext.Model;
using WaveDudeNext.Rendering;
using WaveDudeNext.Runs;

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private bool _fittingBrightness;

    private async void AutoPreviewBrightness(ProbeLayer layer)
    {
        if (_fittingBrightness || _loadedProject is not { } project) return;
        _fittingBrightness = true;
        try
        {
            var plane = layer == ProbeLayer.Field ? FrameLayer.Field : FrameLayer.Intensity;
            double? fit;
            if (_frames is { Count: > 0, Format: FrameFormat.Half } frames)
            {
                var samples = new Half[frames.Width * frames.Height];
                fit = frames.CopyLayer(StoredFrameForTimeline(_viewFrame), plane, samples)
                    ? BrightnessFit.For(samples, ((IFrameSource)frames).KindOf(plane)) : null;
            }
            else
            {
                // Indexed previews have already clipped their values. Compute this frame in half precision
                // using a separate engine so the active simulation and its recordings remain owned by it.
                var spec = RunSpec.Create(project, Rendering.SceneImageLoader.Compose(project, PropertiesEditor.ProjectFolder),
                    true, plane == FrameLayer.Intensity, new PreviewQuality(1, 1, FrameFormat.Half, true));
                int steps = TimelineStep(_viewFrame) + 1;
                fit = await Task.Run(() =>
                {
                    var scene = spec.Image is { } image
                        ? SceneData.FromBgra(image.Bgra, image.Width, image.Height, image.StrideBytes, spec.ImageSources, spec.DampingEdge, spec.Edges == EdgeMode.Open)
                        : SceneData.Empty(spec.Width, spec.Height, spec.DampingEdge, spec.RefractiveIndex, spec.Edges == EdgeMode.Open);
                    var engine = WaveEngineFactory.Create(scene, spec.Engine, spec.PreferGpu, spec.Emitters, out _);
                    try
                    {
                        for (int step = 0; step < steps; step++) engine.AdvanceOneStep();
                        var snapshot = new FrameSnapshot(spec.Shape, spec.TrackIntensity);
                        engine.CaptureFrame(snapshot, spec.TrackIntensity);
                        engine.AwaitFrame(snapshot);
                        return BrightnessFit.For(plane == FrameLayer.Field ? snapshot.Field : snapshot.Intensity,
                            new SnapshotFrameSource(snapshot).KindOf(plane));
                    }
                    finally { (engine as IDisposable)?.Dispose(); }
                });
            }
            if (!ReferenceEquals(project, _loadedProject)) return;
            if (fit is { } brightness)
                (layer == ProbeLayer.Field ? project.Display.Field : project.Display.Intensity).Brightness = brightness;
            else StatusText.Text = UiText.Text("The frame has no nonzero values to fit.");
        }
        catch (Exception ex) { StatusText.Text = "Brightness fit failed: " + ex.Message; }
        finally { _fittingBrightness = false; }
    }
}
