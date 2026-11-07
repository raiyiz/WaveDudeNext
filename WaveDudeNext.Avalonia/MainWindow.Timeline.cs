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

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private readonly List<int> _capturedSteps = new();
    private bool _followSimulation = true;
    private bool _probeFramesAreFull;
    private int TimelineLastFrame => Math.Max(0, Math.Min((_runSteps + _visualizeEvery - 1) / _visualizeEvery - 1, _lastCapturedStep / _visualizeEvery));
    private int TimelineStep(int frame) => Math.Min(frame * _visualizeEvery, Math.Max(0, _lastCapturedStep));
    private int StoredFrameForTimeline(int frame)
    {
        int step = TimelineStep(frame);
        int index = _capturedSteps.BinarySearch(step);
        return index >= 0 ? index : Math.Max(0, ~index - 1);
    }
}
