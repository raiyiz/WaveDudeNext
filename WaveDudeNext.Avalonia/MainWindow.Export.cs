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
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using WaveDudeNext.Export;
using WaveDudeNext.Runs;
using WaveDudeNext.Rendering;

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private ExportJob? _exportJob;
    private int _playIn;
    private int? _playOut;
    private void MarkIn_Click(object? sender, RoutedEventArgs e) { _playIn = _viewFrame; if (_playOut < _playIn) _playOut = null; UpdateRange(); }
    private void MarkOut_Click(object? sender, RoutedEventArgs e) { _playOut = _viewFrame; if (_playIn > _playOut) _playIn = 0; UpdateRange(); }
    private void ClearRange_Click(object? sender, RoutedEventArgs e) { _playIn = 0; _playOut = null; UpdateRange(); }
    private void UpdateRange()
    {
        UpdateTimelineControls();
        StatusText.Text = UiText.Format("Playback/export range: frame {0} to {1}", _playIn, _playOut is { } last ? last.ToString() : UiText.Text("end"));
    }
    private void CancelExport_Click(object? sender, RoutedEventArgs e) => _exportJob?.Cancel();
    private async void ExportRecording_Click(object? sender, RoutedEventArgs e)
    {
        if (_exportJob is { IsActive: true }) { StatusText.Text = UiText.Text("An export is already running."); return; }
        if (_frames is not { Count: > 0 } frames || _loadedProject is not { } project) return;
        if (_playIn > TimelineLastFrame) { StatusText.Text = UiText.Text("The selected range has not been computed yet."); return; }
        _timer.Stop(); _editTimer.Stop(); StopPlayback();
        await _backgroundTask;
        var format = new ComboBox { ItemsSource = ExportFormats.All.Where(f => ExportFormats.IsAvailable(f) || f == ExportFormat.Mp4 && Rendering.FfmpegSink.IsAvailable).ToArray(), SelectedItem = ExportFormat.Png };
        var layer = new ComboBox { ItemsSource = new[] { UiText.Text("Field"), UiText.Text("Intensity") }, SelectedIndex = 0 };
        var quality = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 90 };
        var export = new Button { Content = Controls.IconView.Label("Export", UiText.Text("Export")) };
        var cancel = new Button { Content = Controls.IconView.Label("Exit", UiText.Text("Cancel")) };
        var dialog = new Window { Title = UiText.Text("Export recording"), Width = 440, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        export.Click += (_, _) => dialog.Close(true); cancel.Click += (_, _) => dialog.Close(false);
        dialog.Content = new StackPanel { Margin = new global::Avalonia.Thickness(16), Spacing = 10, Children =
        {
            new TextBlock { Text = UiText.Text("Format (PNG/JPEG/OpenEXR write an image sequence)") }, format,
            new TextBlock { Text = UiText.Text("Layer") }, layer, new TextBlock { Text = UiText.Text("Quality (JPEG/MPEG)") }, quality,
            new TextBlock { Text = UiText.Format("Frames {0}–{1}. OpenEXR: Exr stores I; ExrRoot stores √I.", _playIn, Math.Min(_playOut ?? TimelineLastFrame, TimelineLastFrame)), TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
            new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Children = { export, cancel } }
        } };
        if (!await ShowWorkspaceDialog<bool>(dialog) || !ReferenceEquals(_frames, frames)) return;
        var selected = (ExportFormat)format.SelectedItem!;
        string extension = ExportFormats.Extension(selected);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = UiText.Text("Export recording"), SuggestedFileName = "recording" + extension, DefaultExtension = extension.TrimStart('.'), ShowOverwritePrompt = true,
            FileTypeChoices = new[] { new FilePickerFileType(selected.ToString()) { Patterns = new[] { "*" + extension } } }
        });
        if (file is null || !ReferenceEquals(_frames, frames)) return;
        FrameStore? generated = null;
        try
        {
            if (!_quality.IsFull || selected == ExportFormat.Exr)
            {
                generated = await PrepareExportFrames(selected != ExportFormat.Exr);
                if (generated is null) return;
                frames = generated;
            }
            string path = file.TryGetLocalPath() ?? throw new NotSupportedException("Select a local export destination.");
            bool intensity = layer.SelectedIndex == 1;
            var display = intensity ? project.Display.Intensity : project.Display.Field;
            var scheme = ColorSchemeStore.Find(project.ColorSchemes, display.ColorScheme) ?? ColorSchemeStore.Find(_builtInSchemes, display.ColorScheme);
            var lut = scheme is null ? ColorLut.Grey : ColorLut.Build(scheme, display.BlackLevel);
            var rendering = new ExportLayer(intensity ? FrameLayer.Intensity : FrameLayer.Field, lut, (float)display.Brightness,
                display.OverlayInExport ? _overlayEdges : null, display.OverlayGain,
                (frame, pixels) => Rendering.ExportDecoration.Draw(project, generated is null ? _probeTraces : _exportTraces, intensity ? WaveDudeNext.Model.ProbeLayer.Intensity : WaveDudeNext.Model.ProbeLayer.Field,
                    _engine.Width, _engine.Height, frame, Math.Min(frame * _visualizeEvery, _runSteps), _runSteps, pixels));
            int first = Math.Clamp(_playIn, 0, frames.Count - 1), last = Math.Clamp(_playOut ?? TimelineLastFrame, first, frames.Count - 1);
            int width = _engine.Width, height = _engine.Height, fps = project.Display.Video.Fps, q = (int)(quality.Value ?? 90);
            var target = new ExportTarget(rendering, path, () => selected == ExportFormat.Mp4 && !OperatingSystem.IsWindows() ? new Rendering.FfmpegSink(path, width, height, fps, q) : ExportFormats.Open(selected, path, width, height, fps, q, last - first + 1, first));
            var job = new ExportJob(frames, first, last, new[] { target }, SynchronizationContext.Current);
            _exportJob = job;
            job.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => StatusText.Text = $"Export: {job.FramesDone}/{job.FrameCount} ({job.State})");
            var progress = new Window { Title = UiText.Text("Exporting recording"), Width = 420, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var progressText = new TextBlock { Text = UiText.Text("Starting export…") };
            var stop = new Button { Content = Controls.IconView.Label("Exit", UiText.Text("Cancel export")) };
            stop.Click += (_, _) => job.Cancel();
            progress.Content = new StackPanel { Margin = new global::Avalonia.Thickness(16), Spacing = 12, Children = { progressText, stop } };
            bool finished = false;
            progress.Closing += (_, args) => { if (!finished) { args.Cancel = true; job.Cancel(); } };
            job.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => progressText.Text = UiText.Format("{0}/{1} frames ({2})", job.FramesDone, job.FrameCount, job.State));
            job.Start();
            var shown = ShowWorkspaceDialog(progress);
            await job.Completion;
            finished = true;
            progress.Close();
            await shown;
            StatusText.Text = job.Error is { } error ? "Export failed: " + error.Message : $"Export {job.State}: {path}";
        }
        catch (Exception ex) { StatusText.Text = "Export failed: " + ex.Message; }
        finally { generated?.Dispose(); _exportTraces.Clear(); }
    }
}
