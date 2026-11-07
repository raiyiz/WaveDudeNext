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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;

namespace WaveDudeNext.Avalonia;
public partial class MainWindow
{
    private void FitViews_Click(object? sender, RoutedEventArgs e) { FieldCanvas.ResetView(); IntensityCanvas.ResetView(); }
    private void UiZoom_Click(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse((sender as Control)?.Tag?.ToString(), CultureInfo.InvariantCulture, out double zoom))
        { _desktop.Zoom = Math.Clamp(zoom, 0.8, 1.5); ApplyAppearance(); SaveDesktop(); }
    }
    private void Theme_Click(object? sender, RoutedEventArgs e) { _desktop.Theme = (sender as Control)?.Tag?.ToString() ?? "Dark"; ApplyAppearance(); SaveDesktop(); }
    private void Language_Click(object? sender, RoutedEventArgs e) { _desktop.Language = (sender as Control)?.Tag?.ToString() ?? "EN"; ApplyAppearance(); SaveDesktop(); }
    private void ApplyAppearance()
    {
        RequestedThemeVariant = _desktop.Theme switch { "Light" => ThemeVariant.Light, "Default" => ThemeVariant.Default, _ => ThemeVariant.Dark };
        foreach (var window in _floatingPanes.Values) window.RequestedThemeVariant = RequestedThemeVariant;
        bool light = _desktop.Theme == "Light" || (_desktop.Theme == "Default" && ActualThemeVariant == ThemeVariant.Light);
        string[] keys = { "WorkspaceBackground", "WorkspaceHeader", "WorkspacePane", "WorkspaceCanvas", "WorkspaceBorder", "WorkspaceForeground", "WorkspaceMuted" };
        string[] colors = light ? new[] { "#F3F3F3", "#E8E8E8", "#FAFAFA", "#1E1E1E", "#BFBFBF", "#202020", "#555555" } : new[] { "#2D2D30", "#333337", "#252526", "#1E1E1E", "#3F3F46", "#F1F1F1", "#B0B0B0" };
        for (int i = 0; i < keys.Length; i++) Resources[keys[i]] = new SolidColorBrush(Color.Parse(colors[i]));
        WorkspaceScale.LayoutTransform = new ScaleTransform(Math.Clamp(_desktop.Zoom, 0.8, 1.5), Math.Clamp(_desktop.Zoom, 0.8, 1.5));
        string language = _desktop.Language is "DE" or "FR" or "NL" ? _desktop.Language : "EN";
        var dictionaries = Application.Current!.Resources.MergedDictionaries;
        dictionaries.Clear();
        dictionaries.Add(new ResourceInclude(new Uri("avares://WaveDudeNext.Avalonia/")) { Source = new Uri($"avares://WaveDudeNext.Avalonia/Styles/Strings.{language}.axaml") });
    }
}
