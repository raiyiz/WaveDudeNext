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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WaveDudeNext.Export;

namespace WaveDudeNext.Avalonia.Rendering;

/// <summary>Optional portable H.264 writer using the system ffmpeg executable.</summary>
public sealed class FfmpegSink : IFrameSink
{
    public static string? FindExecutable(string name)
    {
        foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(folder)) continue;
            string path = Path.Combine(folder, OperatingSystem.IsWindows() ? name + ".exe" : name);
            if (File.Exists(path)) return path;
        }
        return null;
    }
    public static bool IsAvailable => FindExecutable("ffmpeg") is not null;
    private readonly Process _process;
    private readonly Task<string> _errors;
    private bool _closed;
    public int Width { get; }
    public int Height { get; }
    public int FrameCount { get; private set; }
    public string Output { get; }
    public FfmpegSink(string path, int width, int height, int fps, int quality)
    {
        Width = width; Height = height; Output = path;
        var start = new ProcessStartInfo(FindExecutable("ffmpeg") ?? throw new NotSupportedException("Install ffmpeg to export MP4."))
        { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (string value in new[] { "-hide_banner", "-loglevel", "error", "-y", "-f", "rawvideo", "-pixel_format", "bgra", "-video_size", $"{width}x{height}", "-framerate", fps.ToString(System.Globalization.CultureInfo.InvariantCulture), "-i", "pipe:0", "-an", "-c:v", "libx264", "-crf", Math.Clamp(51 - quality / 2, 0, 51).ToString(System.Globalization.CultureInfo.InvariantCulture), "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2", "-pix_fmt", "yuv420p", "-movflags", "+faststart", path }) start.ArgumentList.Add(value);
        _process = Process.Start(start) ?? throw new IOException("Could not start ffmpeg.");
        _errors = _process.StandardError.ReadToEndAsync();
    }
    public void Add(ReadOnlySpan<int> pixels)
    {
        if (_closed) throw new InvalidOperationException("The writer is closed.");
        _process.StandardInput.BaseStream.Write(MemoryMarshal.AsBytes(pixels[..checked(Width * Height)]));
        FrameCount++;
    }
    public void Close()
    {
        if (_closed) return;
        _closed = true;
        try
        {
            _process.StandardInput.Close();
            if (!_process.WaitForExit(30000)) { _process.Kill(true); throw new IOException("ffmpeg did not finish within 30 seconds."); }
            string error = _errors.GetAwaiter().GetResult();
            if (_process.ExitCode != 0) throw new IOException("ffmpeg: " + error);
        }
        finally { _process.Dispose(); }
    }
    public void Dispose() => Close();
}
