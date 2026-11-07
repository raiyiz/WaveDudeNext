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
using System.Threading;
using System.Threading.Tasks;

namespace WaveDudeNext.Avalonia;
public partial class MainWindow
{
    private Task _backgroundTask = Task.CompletedTask;
    private readonly object _engineGate = new();
    private CancellationTokenSource? _stepCancel;
    private bool _advancing;
    private int _simulationEpoch;
    private void CancelStepping()
    {
        _stepCancel?.Cancel();
        _simulationEpoch++;
    }
    private async Task UpdateBackground()
    {
        if (_advancing) return;
        _advancing = true;
        var engine = _engine;
        int epoch = _simulationEpoch;
        int count = Math.Min(StoredEvery, Math.Max(0, _runSteps - _step));
        using var cancel = new CancellationTokenSource();
        _stepCancel = cancel;
        try
        {
            int advanced = await Task.Run(() =>
            {
                int done = 0;
                lock (_engineGate)
                    while (done < count && !cancel.IsCancellationRequested)
                    { engine.AdvanceOneStep(); engine.SampleProbes(); done++; }
                return done;
            });
            if (epoch != _simulationEpoch || !ReferenceEquals(engine, _engine)) return;
            _step += advanced;
            if (_step >= _runSteps) _timer.Stop();
            RenderFrame();
        }
        catch (Exception ex) { _timer.Stop(); StatusText.Text = "Simulation failed: " + ex.Message; }
        finally { if (ReferenceEquals(_stepCancel, cancel)) _stepCancel = null; _advancing = false; }
    }
}
