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

namespace WaveDudeNext.Avalonia;
public partial class MainWindow
{
    private readonly Dictionary<Border, Window> _floatingPanes = new();
    private TabControl? _previewTabs;
    private bool _closingWorkspace;
    private void FloatPane_Click(object? sender, RoutedEventArgs e)
    {
        Border? host = (sender as Control)?.Tag?.ToString() switch { "Tree" => TreeHost, "Properties" => PropertiesHost, "Field" => FieldHost, "Intensity" => IntensityHost, _ => null };
        if (host is null || _floatingPanes.ContainsKey(host)) return;
        if (_previewTabs is not null) SideBySide_Click(this, new());
        string key = (sender as Control)!.Tag!.ToString()!;
        var content = host.Child;
        host.Child = null;
        var window = new Window { Title = "WaveDude Next — " + (sender as Control)?.Tag, Width = 650, Height = 650, Content = content, RequestedThemeVariant = RequestedThemeVariant };
        if (_desktop.Panes.TryGetValue(key, out var saved))
        {
            window.Width = Math.Clamp(saved.Width, 240, 3000); window.Height = Math.Clamp(saved.Height, 200, 2000);
            var position = new global::Avalonia.PixelPoint(saved.X, saved.Y);
            if (Screens.ScreenFromPoint(position) is not null) window.Position = position;
        }
        _desktop.Panes[key] = new PanePlacement { Floating = true };
        _floatingPanes.Add(host, window);
        window.Closed += (_, _) =>
        {
            _desktop.Panes[key] = new PanePlacement { Floating = _closingWorkspace, X = window.Position.X, Y = window.Position.Y, Width = window.Width, Height = window.Height };
            window.Content = null; host.Child = content; _floatingPanes.Remove(host);
        };
        window.Show(this);
    }
    private void DockAll_Click(object? sender, RoutedEventArgs e) { foreach (var window in _floatingPanes.Values.ToArray()) window.Close(); }
    private void TabbedPreviews_Click(object? sender, RoutedEventArgs e)
    {
        DockAll_Click(this, new());
        if (_previewTabs is not null) return;
        PreviewGrid.Children.Remove(FieldHost); PreviewGrid.Children.Remove(IntensityHost);
        foreach (var child in PreviewGrid.Children) child.IsVisible = false;
        _previewTabs = new TabControl { ItemsSource = new[] { new TabItem { Header = "Field", Content = FieldHost }, new TabItem { Header = "Intensity", Content = IntensityHost } } };
        Grid.SetColumnSpan(_previewTabs, 3); PreviewGrid.Children.Add(_previewTabs);
        _desktop.TabbedPreviews = true;
    }
    private void SideBySide_Click(object? sender, RoutedEventArgs e)
    {
        if (_previewTabs is null) return;
        foreach (TabItem tab in _previewTabs.ItemsSource!) tab.Content = null;
        PreviewGrid.Children.Remove(_previewTabs); _previewTabs = null;
        foreach (var child in PreviewGrid.Children) child.IsVisible = true;
        PreviewGrid.Children.Add(FieldHost); PreviewGrid.Children.Add(IntensityHost);
        _desktop.TabbedPreviews = false;
    }
}
