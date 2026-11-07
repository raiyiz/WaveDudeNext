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
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WaveDudeNext.Model;
using WaveDudeNext.Rendering;
using WaveDudeNext.Runs;
using WaveDudeNext.Probes;
using WaveDudeNext.Export;

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private FrameStore? _frames;
    private ProbeCollector? _probeCollector;
    private readonly System.Collections.Generic.Dictionary<Probe, ProbeTrace> _probeTraces = new();
    private int _lastCapturedStep = -1;
    private int _viewFrame;
    private bool _settingTimeline;
    private Half[] _samples = [];
    private int[] _pixels = [];
    private ColorLut? _fieldLut;
    private ColorLut? _intensityLut;
    private byte[]? _overlayEdges;
    private Project? _previewProject;
    private DispatcherTimer _playbackTimer = null!;
    private DispatcherTimer _editTimer = null!;
    private readonly System.Collections.Generic.List<ColorScheme> _builtInSchemes = ColorSchemeStore.LoadBuiltIn(null);
    private object? _selectedObject;
    private readonly System.Collections.Generic.List<object> _selection = new();
    private bool _syncingSelection;

    private void OnProjectTreeCommand(string command, object? target)
    {
        if (target is not null && command is "MoveUp" or "MoveDown" or "Remove")
        {
            ProjectTreeControl.SelectModel(target);
            SelectObject(target);
        }
        var args = new RoutedEventArgs();
        switch (command)
        {
            case "AddSource": AddSource_Click(this, args); break;
            case "AddProbe": AddProbe_Click(this, args); break;
            case "AddScheme": AddScheme_Click(this, args); break;
            case "AddPart": AddPart_Click(new Button { Tag = target }, args); break;
            case "MoveUp": MoveObject_Click(new Button { Tag = "-1" }, args); break;
            case "MoveDown": MoveObject_Click(new Button { Tag = "1" }, args); break;
            case "Remove": Delete_Click(this, args); break;
        }
    }

    private void InitializeWorkspace()
    {
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _playbackTimer.Tick += (_, _) =>
        {
            if (_frames is null || _frames.Count == 0) return;
            int next = _viewFrame + 1;
            if (next > Math.Min(_playOut ?? TimelineLastFrame, TimelineLastFrame))
            {
                if (LoopPlayback.IsChecked == true) next = _playIn;
                else { StopPlayback(); return; }
            }
            ShowStoredFrame(next);
        };
        _editTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _editTimer.Tick += (_, _) =>
        {
            if (FieldCanvas.IsManipulating || IntensityCanvas.IsManipulating) return;
            _editTimer.Stop();
            if (AutoApply.IsChecked == true) ApplyPendingChanges();
        };
        ProjectTreeControl.CommandRequested += OnProjectTreeCommand;
        ProjectTreeControl.SelectedModelsChanged += models =>
        {
            if (_syncingSelection) return;
            _selection.Clear(); _selection.AddRange(models);
            UpdateSelection(ProjectTreeControl.SelectedModel);
        };
        foreach (var canvas in new[] { FieldCanvas, IntensityCanvas })
        {
            canvas.SelectionRequested += (model, toggle) =>
            {
                if (!toggle && (model is null || !_selection.Contains(model))) _selection.Clear();
                if (model is not null)
                {
                    if (toggle && _selection.Contains(model)) _selection.Remove(model);
                    else if (!_selection.Contains(model)) _selection.Add(model);
                }
                _syncingSelection = true;
                ProjectTreeControl.SelectModel(model);
                _syncingSelection = false;
                UpdateSelection(model);
            };
            canvas.EditCompleted += () => { if (AutoApply.IsChecked == true) ApplyPendingChanges(); };
        }
        AddHandler(KeyDownEvent, OnWorkspaceKeyDown, RoutingStrategies.Bubble);
        InitializeDesktop();
        InitializeTimeline();
        InitializeDrop();
        FieldCanvas.ViewChanged += () => FieldImage.RenderTransform = new global::Avalonia.Media.MatrixTransform(FieldCanvas.ImageTransform);
        IntensityCanvas.ViewChanged += () => IntensityImage.RenderTransform = new global::Avalonia.Media.MatrixTransform(IntensityCanvas.ImageTransform);
        PopulateExamples();
        // Open a real editable project at startup, rather than the original unattached engine demo.
        var project = new Project();
        project.Scene.Width = 640;
        project.Scene.Height = 360;
        project.Simulation.Steps = 1500;
        project.AddSource(160, 180);
        InstallProject(project, null);
        ProjectTreeControl.SelectModel(project);
    }

    private void SelectObject(object? model)
    {
        if (_syncingSelection) return;
        _selection.Clear();
        if (model is not null) _selection.Add(model);
        UpdateSelection(model);
    }
    private void UpdateSelection(object? model)
    {
        _selectedObject = model;
        PropertiesEditor.SelectedObject = model;
        foreach (var canvas in new[] { FieldCanvas, IntensityCanvas })
        {
            canvas.SelectedObject = model;
            canvas.Selection = _selection;
            canvas.InvalidateVisual();
        }
    }

    private void InstallProject(Project project, string? path)
    {
        LoadSimulation(project, path is null ? null : Path.GetDirectoryName(path));
        _loadedProject = project;
        foreach (var bar in _overlayBars) bar.SynchronizeState();
        _projectPath = path;
        if (path is not null) RememberProject(path);
        PropertiesEditor.Project = project;
        PropertiesEditor.ProjectFolder = path is null ? null : Path.GetDirectoryName(path);
        ProjectTreeControl.LoadProject(project);
        SelectObject(null);
        AttachProjectTracking(project);
        SynchronizeProbeViewports();
        Title = "WaveDude Next — " + (path is null ? "Untitled" : Path.GetFileNameWithoutExtension(path));
    }

    private void PopulateExamples()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Examples");
        if (!Directory.Exists(folder)) return;
        foreach (string path in Directory.EnumerateFiles(folder, "*.wdproj").OrderBy(Path.GetFileName))
        {
            var item = new MenuItem { Header = Path.GetFileNameWithoutExtension(path).Replace('_', ' ') };
            item.Click += async (_, _) =>
            {
                if (!await ConfirmProjectReplacement()) return;
                try { InstallProject(Project.Load(path), path); }
                catch (Exception ex) { StatusText.Text = "Could not open example: " + ex.Message; }
            };
            ExamplesMenu.Items.Add(item);
        }
    }

    private async void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        if (await ConfirmProjectReplacement()) InstallProject(new Project(), null);
    }

    private void AddSource_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is { } project) FinishObjectEdit(project.AddSource(_engine.Width / 2, _engine.Height / 2));
    }
    private void AddProbe_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is { } project) FinishObjectEdit(project.AddProbe(_engine.Width / 2, _engine.Height / 2));
    }
    private void AddScheme_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is { } project) FinishObjectEdit(project.AddColorScheme());
    }
    private void AddPart_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is not { } project || (sender as Control)?.Tag is not string name) return;
        var type = typeof(OpticalPart).Assembly.GetType("WaveDudeNext.Model." + name);
        if (type is not null) FinishObjectEdit(project.AddPart(type, _engine.Width / 2, _engine.Height / 2));
    }
    private void Duplicate_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is not { } p) return;
        var copies = _selection.ToArray().Select(item => item switch
        {
            Source source => (object)p.Duplicate(source), OpticalPart part => p.Duplicate(part),
            Probe probe => p.Duplicate(probe), ColorScheme scheme => p.Duplicate(scheme), _ => null
        }).Where(item => item is not null).Cast<object>().ToArray();
        if (copies.Length > 0) { FinishObjectEdit(copies[^1]); _selection.Clear(); _selection.AddRange(copies); UpdateSelection(copies[^1]); }
    }
    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is not { } p) return;
        bool removed = false;
        foreach (object item in _selection.ToArray())
            removed |= item switch
            {
                Source source => p.Remove(source), OpticalPart part => p.Remove(part),
                Probe probe => p.Remove(probe), ColorScheme scheme => p.Remove(scheme), _ => false
            };
        if (removed) FinishObjectEdit(null);
    }
    private void MoveObject_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is not { } p || !int.TryParse((sender as Control)?.Tag?.ToString(), out int direction)) return;
        IList? collection = _selectedObject switch
        {
            Source => p.Sources, OpticalPart => p.Parts, Probe => p.Probes, ColorScheme => p.ColorSchemes, _ => null
        };
        if (collection is null || _selectedObject is null) return;
        int index = collection.IndexOf(_selectedObject);
        int destination = Math.Clamp(index + direction, 0, collection.Count - 1);
        switch (_selectedObject)
        {
            case Source source: p.Move(source, destination); break;
            case OpticalPart part: p.Move(part, destination); break;
            case Probe probe: p.Move(probe, destination); break;
            case ColorScheme scheme: p.Move(scheme, destination); break;
        }
        FinishObjectEdit(_selectedObject);
    }
    private void FinishObjectEdit(object? selected)
    {
        ProjectTreeControl.LoadProject(_loadedProject);
        ProjectTreeControl.SelectModel(selected);
        SelectObject(selected);
        ApplyPendingChanges();
    }
    private void ApplyPendingChanges()
    {
        bool resume = _timer.IsEnabled;
        ApplyChanges_Click(this, new RoutedEventArgs());
        if (resume) _timer.Start();
        FieldCanvas.InvalidateVisual();
        IntensityCanvas.InvalidateVisual();
        if (_frames is { Count: > 0 }) ShowStoredFrame(_viewFrame);
    }

    private void ResetTimeline()
    {
        StopPlayback();
        _playIn = 0; _playOut = null;
        _frames?.Dispose();
        _frames = FrameStore.Create(Path.Combine(Path.GetTempPath(), "wavedude-" + Guid.NewGuid().ToString("N") + ".frames"),
            new WaveDudeNext.Engine.FrameShape(_engine.Width, _engine.Height, _quality.ResolutionDivisor, _quality.Format,
                (float)_previewProject!.Display.Field.Brightness, (float)_previewProject.Display.Intensity.Brightness), FrameLayers.Field | FrameLayers.Intensity,
            (_runSteps + StoredEvery - 1) / StoredEvery + 1, StoredEvery, Math.Min(128L * 1024 * 1024, FrameMemory.Budget(10)));
        _samples = new Half[_frames.Width * _frames.Height];
        _indices = new byte[_samples.Length];
        _pixels = new int[_samples.Length];
        var probeSpecs = _previewProject!.Probes.Where(probe => probe.Active).Select(ProbeSpec.From).ToArray();
        var sampler = ProbeSampler.Build(probeSpecs, _engine.Width, _engine.Height, _engine.Rim);
        _engine.SetProbes(sampler);
        _probeCollector = new ProbeCollector(sampler);
        _probeTraces.Clear();
        FieldCanvas.ProfileFrameStride = IntensityCanvas.ProfileFrameStride = 1;
        for (int i = 0; i < probeSpecs.Length; i++) _probeTraces[probeSpecs[i].Origin!] = _probeCollector.Traces[i];
        FieldCanvas.Traces = IntensityCanvas.Traces = _probeTraces;
        FieldCanvas.Layer = ProbeLayer.Field;
        IntensityCanvas.Layer = ProbeLayer.Intensity;
        _lastCapturedStep = -1;
        _capturedSteps.Clear(); _simulationPaused = false; _followSimulation = true; _probeFramesAreFull = false;
        _viewFrame = 0;
        _fieldLut = _intensityLut = null;
        _settingTimeline = true;
        TimelineSlider.Maximum = PlannedLastFrame;
        TimelineSlider.Available = 0;
        TimelineSlider.RangeStart = 0; TimelineSlider.RangeEnd = -1;
        TimelineSlider.Value = 0;
        _settingTimeline = false;
    }
    private void CaptureFrame()
    {
        lock (_engineGate) CaptureFrameCore();
    }
    private void CaptureFrameCore()
    {
        _engine.PrepareFrame(true, true);
        if (_frames is null || _lastCapturedStep == _step) return;
        _frames.Write(_frames.Count, _engine.FieldData, _engine.IntensityData, _engine.Stride, _engine.Rim);
        _probeCollector?.Collect(_engine, _step);
        _probeCollector?.CollectProfiles(_engine, _step);
        _probeCollector?.TakeForUi();
        _lastCapturedStep = _step;
        _capturedSteps.Add(_step);
        _settingTimeline = true;
        TimelineSlider.Available = TimelineLastFrame;
        _settingTimeline = false;
    }
    private void RenderFrame()
    {
        CaptureFrame();
        ShowStoredFrame(_followSimulation ? TimelineLastFrame : _viewFrame);
        StatusText.Text = $"{_engine.Description} | {_engine.Width} × {_engine.Height} | Step {_step} / {_runSteps}";
    }
    private void ShowStoredFrame(int frame)
    {
        if (_frames is not { Count: > 0 } frames || _previewProject is not { } project) return;
        foreach (var bar in _overlayBars) bar.SynchronizeState();
        _viewFrame = Math.Clamp(frame, 0, TimelineLastFrame);
        FieldCanvas.Frame = IntensityCanvas.Frame = _probeFramesAreFull ? _viewFrame : StoredFrameForTimeline(_viewFrame);
        FieldCanvas.Step = IntensityCanvas.Step = TimelineStep(_viewFrame);
        FieldCanvas.TotalSteps = IntensityCanvas.TotalSteps = _runSteps;
        DrawStoredPlane(_fieldBitmap, FrameLayer.Field, project.Display.Field, ref _fieldLut);
        DrawStoredPlane(_intensityBitmap, FrameLayer.Intensity, project.Display.Intensity, ref _intensityLut);
        FieldImage.InvalidateVisual();
        IntensityImage.InvalidateVisual();
        FieldCanvas.InvalidateVisual();
        IntensityCanvas.InvalidateVisual();
        SynchronizeProbeViewports();
        _settingTimeline = true;
        TimelineSlider.Value = _viewFrame;
        _settingTimeline = false;
        UpdateTimelineControls();
        FrameText.Text = $"Frame {_viewFrame} / {PlannedLastFrame}  ·  stored {frames.Count}  ·  {frames.Width} × {frames.Height}, {(_quality.Format == WaveDudeNext.Engine.FrameFormat.Index ? "8-bit" : "16-bit")}  ·  RAM {frames.FramesInMemory}, disk {frames.FramesOnDisk}{(frames.RamFrames < frames.Capacity ? " (disk cache required)" : "")}  ·  Step {TimelineStep(_viewFrame)}";
    }
    private void DrawStoredPlane(WriteableBitmap bitmap, FrameLayer layer, DisplayLayer display, ref ColorLut? lut)
    {
        if (_frames is null) return;
        if (_frames.Format == WaveDudeNext.Engine.FrameFormat.Index) { if (!_frames.CopyLayer(StoredFrameForTimeline(_viewFrame), layer, _indices.AsSpan())) return; }
        else if (!_frames.CopyLayer(StoredFrameForTimeline(_viewFrame), layer, _samples.AsSpan())) return;
        if (lut is null)
        {
            var scheme = ColorSchemeStore.Find(_previewProject!.ColorSchemes, display.ColorScheme)
                ?? ColorSchemeStore.Find(_builtInSchemes, display.ColorScheme);
            lut = scheme is null ? ColorLut.Grey : ColorLut.Build(scheme, display.BlackLevel);
        }
        if (_frames.Format == WaveDudeNext.Engine.FrameFormat.Index) FrameRenderer.RenderIndexed(_indices, _frames.Width, _frames.Height, lut, _pixels);
        else FrameRenderer.Render(_samples, _frames.Width, _frames.Height, lut,
            ((IFrameSource)_frames).KindOf(layer), (float)display.Brightness, _pixels);
        var overlays = layer == FrameLayer.Field ? FieldCanvas.Overlays : IntensityCanvas.Overlays;
        if (overlays.Drawings && overlays.Edges && display.Overlay && PreviewEdges() is { } edges)
            GlowingEdges.BlendAdditive(_pixels, _frames.Width, _frames.Height, edges, display.OverlayGain);
        using var target = bitmap.Lock();
        for (int y = 0; y < _frames.Height; y++)
            Marshal.Copy(_pixels, y * _frames.Width, target.Address + y * target.RowBytes, _frames.Width);
    }
    private void Timeline_Changed(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_settingTimeline || _frames is null) return;
        SeekFrame((int)Math.Round(e.NewValue));
    }
    private void Seek_Click(object? sender, RoutedEventArgs e)
    {
        if (_frames is null) return;
        _timer.Stop();
        StopPlayback(); _followSimulation = false;
        int frame = (sender as Control)?.Tag?.ToString() switch
        {
            "First" => 0, "Previous" => _viewFrame - 1, "Next" => _viewFrame + 1, _ => TimelineLastFrame
        };
        ShowStoredFrame(frame);
    }
    private void PlayPause_Click(object? sender, RoutedEventArgs e)
    {
        if (_playbackTimer.IsEnabled) { StopPlayback(); return; }
        if (_frames is not { Count: > 1 } || _playIn > TimelineLastFrame) return;
        _timer.Stop(); _followSimulation = false;
        if (_viewFrame < _playIn || _viewFrame >= Math.Min(_playOut ?? TimelineLastFrame, TimelineLastFrame)) ShowStoredFrame(_playIn);
        double fps = _loadedProject?.Display.Video.Fps ?? 30;
        _playbackTimer.Interval = TimeSpan.FromSeconds(1 / (fps * (double)(PlaybackSpeed.Value ?? 1)));
        _playbackTimer.Start();
        UpdateTimelineControls();
    }
    private void StopPlayback()
    {
        _playbackTimer?.Stop();
        if (PlayButton is not null) UpdateTimelineControls();
    }
    private async void ExportFrame_Click(object? sender, RoutedEventArgs e)
    {
        _timer.Stop(); _editTimer.Stop(); StopPlayback();
        await _backgroundTask;
        try
        {
            bool intensity = (sender as Control)?.Tag?.ToString() == "Intensity";
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = UiText.Text("Export current frame"), SuggestedFileName = $"{(intensity ? "intensity" : "field")}_{_viewFrame:D5}.png",
                DefaultExtension = "png", ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("PNG image") { Patterns = ["*.png"] }]
            });
            if (file is null) return;
            var rendered = await RenderCurrentFrameForExport(intensity);
            if (rendered is not { } image) return;
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            PngEncoder.Write(stream, image.Pixels, image.Width, image.Height);
            StatusText.Text = "Frame exported: " + file.Name;
        }
        catch (Exception ex) { StatusText.Text = "Could not export frame: " + ex.Message; }
    }
    private async void ExportProbe_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedObject is not Probe probe || !_probeTraces.TryGetValue(probe, out var trace))
        {
            StatusText.Text = UiText.Text("Select a recorded probe in the project tree first.");
            return;
        }
        bool running = _timer.IsEnabled;
        _timer.Stop();
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = UiText.Text("Export probe recording"), SuggestedFileName = ProbeCsv.FileName("Project", probe),
                DefaultExtension = "csv", ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }]
            });
            if (file is null) return;
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            using var writer = new StreamWriter(stream);
            int rows = ProbeCsv.Write(writer, probe, trace, _probeFramesAreFull ? _visualizeEvery : StoredEvery, 1);
            StatusText.Text = $"Exported {rows} probe rows to {file.Name}";
        }
        catch (Exception ex) { StatusText.Text = "Could not export probe: " + ex.Message; }
        finally { if (running) _timer.Start(); }
    }

    private async Task<bool> ConfirmProjectReplacement()
    {
        _editTimer.Stop();
        if (_loadedProject is null || (!_hasPendingChanges && _projectHistory?.IsAtSaved != false)) return true;
        var dialog = new Window { Title = UiText.Text("Unsaved changes"), Width = 420, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var save = new Button { Content = UiText.Text("Save") };
        var discard = new Button { Content = UiText.Text("Discard") };
        var cancel = new Button { Content = UiText.Text("Cancel") };
        save.Click += (_, _) => dialog.Close("Save");
        discard.Click += (_, _) => dialog.Close("Discard");
        cancel.Click += (_, _) => dialog.Close("Cancel");
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16), Spacing = 16,
            Children = { new TextBlock { Text = UiText.Text("Save changes to this project?") }, new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
                Children = { save, discard, cancel }
            } }
        };
        string? answer = await ShowWorkspaceDialog<string?>(dialog);
        if (answer == "Save") return await SaveProjectAsync(false);
        return answer == "Discard";
    }
    private bool _allowClose;
    private async void Exit_Click(object? sender, RoutedEventArgs e)
    {
        if (await ConfirmProjectReplacement()) { _allowClose = true; Close(); }
    }
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (!_allowClose && (_hasPendingChanges || _projectHistory?.IsAtSaved == false))
        {
            e.Cancel = true;
            base.OnClosing(e);
            if (await ConfirmProjectReplacement()) { _allowClose = true; Close(); }
            return;
        }
        base.OnClosing(e);
        if (e.Cancel) return;
        _desktop.DockLayout = _dock.CaptureLayout();
        _closingWorkspace = true;
        _dock.CloseWindows();
    }
    private void OnWorkspaceKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        // Native text editing keeps its own Undo, Delete and clipboard shortcuts.
        bool editing = e.Source is TextBox or NumericUpDown or ComboBox || (e.Source as Control)?.FindAncestorOfType<TextBox>() is not null || (e.Source as Control)?.FindAncestorOfType<ComboBox>() is not null;
        if (ctrl && e.Key == Key.S)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) SaveProjectAs_Click(this, new RoutedEventArgs());
            else SaveProject_Click(this, new RoutedEventArgs());
        }
        else if (ctrl && e.Key == Key.O) OpenProject_Click(this, new RoutedEventArgs());
        else if (ctrl && e.Key == Key.N) NewProject_Click(this, new RoutedEventArgs());
        else if (!editing && ctrl && e.Key == Key.Z) Undo_Click(this, new RoutedEventArgs());
        else if (!editing && ctrl && e.Key == Key.Y) Redo_Click(this, new RoutedEventArgs());
        else if (!editing && ctrl && e.Key == Key.C) Copy_Click(this, new());
        else if (!editing && ctrl && e.Key == Key.X) Cut_Click(this, new());
        else if (!editing && ctrl && e.Key == Key.V) Paste_Click(this, new());
        else if (e.Key == Key.F1) Manual_Click(this, new());
        else if (!editing && ctrl && e.Key == Key.D) Duplicate_Click(this, new RoutedEventArgs());
        else if (!editing && e.Key == Key.Delete) Delete_Click(this, new RoutedEventArgs());
        else if (e.Key == Key.F5) TimelineRun_Click(this, new RoutedEventArgs());
        else if (e.Key == Key.F6) Pause_Click(this, new RoutedEventArgs());
        else if (e.Key == Key.F7) PlayPause_Click(this, new RoutedEventArgs());
        else if (!editing && !e.Handled && HandleTimelineShortcut(e)) { }
        else return;
        e.Handled = true;
    }
}
