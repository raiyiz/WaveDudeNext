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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using WaveDudeNext.Rendering;

namespace WaveDudeNext.Avalonia.Controls;

public sealed class ColorPickerButton : Button
{
    public static readonly StyledProperty<Rgb> ColorProperty = AvaloniaProperty.Register<ColorPickerButton, Rgb>(nameof(Color), defaultBindingMode: BindingMode.TwoWay);
    public Rgb Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public ColorPickerButton() { Width = 30; UpdateSwatch(); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColorProperty) UpdateSwatch();
    }
    private void UpdateSwatch() => Background = new SolidColorBrush(global::Avalonia.Media.Color.FromRgb(Color.R, Color.G, Color.B));
    protected override void OnClick()
    {
        base.OnClick();
        var red = new Slider { Minimum = 0, Maximum = 255, Value = Color.R, IsSnapToTickEnabled = true, TickFrequency = 1 };
        var green = new Slider { Minimum = 0, Maximum = 255, Value = Color.G, IsSnapToTickEnabled = true, TickFrequency = 1 };
        var blue = new Slider { Minimum = 0, Maximum = 255, Value = Color.B, IsSnapToTickEnabled = true, TickFrequency = 1 };
        void Update() => SetCurrentValue(ColorProperty, new Rgb((byte)red.Value, (byte)green.Value, (byte)blue.Value));
        red.ValueChanged += (_, _) => Update(); green.ValueChanged += (_, _) => Update(); blue.ValueChanged += (_, _) => Update();
        var flyout = new Flyout { Content = new StackPanel
        {
            Width = 220, Spacing = 4,
            Children = { new TextBlock { Text = "Red" }, red, new TextBlock { Text = "Green" }, green, new TextBlock { Text = "Blue" }, blue }
        } };
        flyout.ShowAt(this);
    }
}
