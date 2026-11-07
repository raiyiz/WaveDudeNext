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
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using WaveDudeNext.Model;
using WaveDudeNext.Rendering;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>Project and shipped colour schemes, filtered for the edited output.</summary>
public sealed class ColorSchemePicker : ComboBox
{
    public static readonly StyledProperty<string?> SelectedNameProperty =
        AvaloniaProperty.Register<ColorSchemePicker, string?>(nameof(SelectedName), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<Project?> ProjectProperty =
        AvaloniaProperty.Register<ColorSchemePicker, Project?>(nameof(Project));
    public static readonly StyledProperty<ColorSchemeKind> KindProperty =
        AvaloniaProperty.Register<ColorSchemePicker, ColorSchemeKind>(nameof(Kind));
    public string? SelectedName { get => GetValue(SelectedNameProperty); set => SetValue(SelectedNameProperty, value); }
    public Project? Project { get => GetValue(ProjectProperty); set => SetValue(ProjectProperty, value); }
    public ColorSchemeKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }
    private static readonly System.Collections.Generic.List<ColorScheme> BuiltIn = ColorSchemeStore.LoadBuiltIn(null);
    private Project? _observedProject;
    private bool _refreshing;

    public ColorSchemePicker()
    {
        ItemTemplate = new FuncDataTemplate<ColorScheme>((scheme, _) =>
        {
            if (scheme is null) return new TextBlock();
            var gradient = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative) };
            foreach (var stop in scheme.SortedStops())
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(stop.Color.R, stop.Color.G, stop.Color.B), stop.Position));
            return new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 6,
                Children = { new Border { Width = 48, Height = 12, Background = gradient, VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = scheme.Name } }
            };
        });
        SelectionChanged += (_, _) =>
        {
            if (!_refreshing && SelectedItem is ColorScheme scheme) SetCurrentValue(SelectedNameProperty, scheme.Name);
        };
        AttachedToVisualTree += (_, _) => { Observe(); Refresh(); };
        DetachedFromVisualTree += (_, _) => Unobserve();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ProjectProperty) { Observe(); Refresh(); }
        else if (change.Property == KindProperty || change.Property == SelectedNameProperty) Refresh();
    }
    private void Observe()
    {
        Unobserve();
        _observedProject = Project;
        if (_observedProject is not null) _observedProject.ColorSchemes.CollectionChanged += OnSchemesChanged;
    }
    private void Unobserve()
    {
        if (_observedProject is not null) _observedProject.ColorSchemes.CollectionChanged -= OnSchemesChanged;
        _observedProject = null;
    }
    private void OnSchemesChanged(object? sender, NotifyCollectionChangedEventArgs args) => Refresh();
    private void Refresh()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            var schemes = (Project?.ColorSchemes.AsEnumerable() ?? []).Concat(BuiltIn)
                .Where(scheme => scheme.Kind == ColorSchemeKind.Unspecified || scheme.Kind == Kind)
                .DistinctBy(scheme => scheme.Name).ToArray();
            ItemsSource = schemes;
            SelectedItem = schemes.FirstOrDefault(scheme => scheme.Name == SelectedName);
        }
        finally { _refreshing = false; }
    }
}
