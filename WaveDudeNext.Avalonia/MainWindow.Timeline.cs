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
using Avalonia.Controls;

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private readonly List<int> _capturedSteps = new();
    private bool _followSimulation = true;
    private bool _probeFramesAreFull;
    private int TimelineLastFrame => Math.Max(0, Math.Min(PlannedLastFrame, _lastCapturedStep / _visualizeEvery));
    private int TimelineStep(int frame) => Math.Min(frame * _visualizeEvery, Math.Max(0, _lastCapturedStep));
    private int StoredFrameForTimeline(int frame)
    {
        int step = TimelineStep(frame);
        int index = _capturedSteps.BinarySearch(step);
        return index >= 0 ? index : Math.Max(0, ~index - 1);
    }
}

public partial class MainWindow
{
    private int PlannedLastFrame => Math.Max(0, (_runSteps - 1) / Math.Max(1, _visualizeEvery));
    private bool _startingTimelineRun;
    private bool _simulationPaused;
    private void InitializeTimeline()
    {
        // The compact number template can attach after an unchanged initial Value was formatted.
        PlaybackSpeed.Loaded += (_, _) => EnsureNumberText(PlaybackSpeed, "0.##");
        FramePosition.Loaded += (_, _) => EnsureNumberText(FramePosition, "0");
        TimelineSlider.RangeEdited += (start, end) =>
        {
            _playIn = Math.Clamp(start, 0, PlannedLastFrame);
            _playOut = end < 0 ? null : Math.Clamp(end, _playIn, PlannedLastFrame);
            UpdateRange();
        };
        UpdateTimelineControls();
    }
    private static void EnsureNumberText(NumericUpDown control, string format)
    {
        if (string.IsNullOrWhiteSpace(control.Text)) control.Text = (control.Value ?? 0).ToString(format, System.Globalization.CultureInfo.CurrentCulture);
    }
    private void UpdateTimelineControls()
    {
        if (FramePosition is null) return;
        _settingTimeline = true;
        try
        {
            TimelineSlider.Maximum = PlannedLastFrame;
            TimelineSlider.Available = TimelineLastFrame;
            TimelineSlider.RangeStart = _playIn;
            TimelineSlider.RangeEnd = _playOut ?? -1;
            TimelineSlider.Value = _viewFrame;
            FramePosition.Maximum = TimelineLastFrame;
            FramePosition.Value = _viewFrame;
        }
        finally { _settingTimeline = false; }
        FramePositionText.Text = "/ " + PlannedLastFrame.ToString(System.Globalization.CultureInfo.CurrentCulture);
        StepText.Text = UiText.Format("step {0}", TimelineStep(_viewFrame));
        LiveFollow.IsChecked = _followSimulation;
        bool frames = _frames is { Count: > 0 };
        PlayButton.IsEnabled = _playIn <= TimelineLastFrame && _frames is { Count: > 1 } && _playbackTimer?.IsEnabled != true;
        PausePlaybackButton.IsEnabled = _playbackTimer?.IsEnabled == true;
        TimelinePauseRunButton.IsEnabled = _timer.IsEnabled || (_simulationPaused && _step < _runSteps);
        if (TimelinePauseRunButton.Content is Controls.IconView pauseIcon) pauseIcon.Drawing = _simulationPaused ? "RunSimulation" : "PauseSimulation";
        string pauseTipKey = _simulationPaused ? "TipPreviewResumeRun" : "TipPreviewPauseRun";
        if (this.TryFindResource(pauseTipKey, out var pauseTip) && pauseTip is string label)
        { global::Avalonia.Controls.ToolTip.SetTip(TimelinePauseRunButton, label); global::Avalonia.Automation.AutomationProperties.SetName(TimelinePauseRunButton, label); }
        TimelineRunButton.IsEnabled = !_startingTimelineRun;
        FramePosition.IsEnabled = frames; TimelineSlider.IsEnabled = frames;
        StepBackButton.IsEnabled = frames && _viewFrame > 0;
        StepForwardButton.IsEnabled = frames && _viewFrame < TimelineLastFrame;
        SetInButton.IsEnabled = SetOutButton.IsEnabled = frames;
        if (_dock is null) return;
        ShowFieldButton.IsChecked = _dock.IsPaneVisible("Field");
        ShowIntensityButton.IsChecked = _dock.IsPaneVisible("Intensity");
        bool? stacked = _dock.AreStacked("Field", "Intensity");
        SideBySideButton.IsChecked = stacked == false;
        StackedButton.IsChecked = stacked == true;
    }
    private void FramePosition_Changed(object? sender, global::Avalonia.Controls.NumericUpDownValueChangedEventArgs e)
    {
        if (_settingTimeline || _frames is null) return;
        SeekFrame((int)(FramePosition.Value ?? 0));
    }
    private void SeekFrame(int frame)
    {
        _timer.Stop(); StopPlayback(); _followSimulation = false;
        ShowStoredFrame(frame);
    }
    private void Play_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    { if (!_playbackTimer.IsEnabled) PlayPause_Click(sender, e); }
    private void PausePlayback_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) => StopPlayback();
    private void LiveFollow_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _followSimulation = LiveFollow.IsChecked == true;
        if (_followSimulation) { StopPlayback(); ShowStoredFrame(TimelineLastFrame); }
        UpdateTimelineControls();
    }
    private void LoopPlayback_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) => UpdateTimelineControls();
    private void PlaybackSpeed_Changed(object? sender, global::Avalonia.Controls.NumericUpDownValueChangedEventArgs e)
    {
        if (_playbackTimer is null) return;
        double fps = _loadedProject?.Display.Video.Fps ?? 30;
        _playbackTimer.Interval = TimeSpan.FromSeconds(1 / (fps * (double)(PlaybackSpeed.Value ?? 1)));
    }
    private void PlaybackPreset_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (decimal.TryParse((sender as global::Avalonia.Controls.Control)?.Tag?.ToString(), System.Globalization.CultureInfo.InvariantCulture, out decimal speed)) PlaybackSpeed.Value = speed;
        SpeedPresetButton.Flyout?.Hide();
    }
    private async void TimelineRun_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_startingTimelineRun) return;
        _startingTimelineRun = true; UpdateTimelineControls();
        try
        {
            _timer.Stop(); _stepCancel?.Cancel(); await _backgroundTask;
            int start = _playIn; int? end = _playOut;
            if (_hasPendingChanges) ApplyPendingChanges();
            Reset_Click(sender, e);
            _playIn = Math.Clamp(start, 0, PlannedLastFrame);
            _playOut = end is { } last ? Math.Clamp(last, _playIn, PlannedLastFrame) : null;
            Start_Click(sender, e);
        }
        catch (Exception error) { StatusText.Text = "Could not start simulation: " + error.Message; }
        finally { _startingTimelineRun = false; UpdateTimelineControls(); }
    }
    private void TogglePreview_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not global::Avalonia.Controls.Primitives.ToggleButton button || button.Tag is not string id) return;
        if (button.IsChecked != true)
        {
            if (_dock.AreStacked("Field", "Intensity") is bool stacked) _desktop.PreviewStacked = stacked;
            _dock.Hide(id);
        }
        else
        {
            string other = id == "Field" ? "Intensity" : "Field";
            if (_dock.IsPaneVisible(other))
                _dock.Dock(id, other, _desktop.PreviewStacked
                    ? id == "Field" ? Docking.DockSide.Top : Docking.DockSide.Bottom
                    : id == "Field" ? Docking.DockSide.Left : Docking.DockSide.Right);
            else _dock.Dock(id, _dock.IsPaneVisible("Properties") ? "Properties" : "Tree", _dock.IsPaneVisible("Properties") ? Docking.DockSide.Left : Docking.DockSide.Right);
        }
        UpdateTimelineControls();
    }
    private void StackedPreviews_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _desktop.PreviewStacked = true;
        _dock.Show("Field"); _dock.Dock("Intensity", "Field", Docking.DockSide.Bottom);
        _desktop.TabbedPreviews = false; UpdateTimelineControls();
    }
    private bool HandleTimelineShortcut(global::Avalonia.Input.KeyEventArgs e)
    {
        if (e.KeyModifiers != global::Avalonia.Input.KeyModifiers.None) return false;
        var args = new global::Avalonia.Interactivity.RoutedEventArgs();
        switch (e.Key)
        {
            case global::Avalonia.Input.Key.Space: PlayPause_Click(this, args); break;
            case global::Avalonia.Input.Key.OemComma: SeekFrame(_viewFrame - 1); break;
            case global::Avalonia.Input.Key.OemPeriod: SeekFrame(_viewFrame + 1); break;
            case global::Avalonia.Input.Key.I: MarkIn_Click(this, args); break;
            case global::Avalonia.Input.Key.O: MarkOut_Click(this, args); break;
            case global::Avalonia.Input.Key.X: ClearRange_Click(this, args); break;
            case global::Avalonia.Input.Key.L: LoopPlayback.IsChecked = LoopPlayback.IsChecked != true; break;
            case global::Avalonia.Input.Key.F: LiveFollow.IsChecked = LiveFollow.IsChecked != true; LiveFollow_Click(this, args); break;
            default: return false;
        }
        return true;
    }
}
