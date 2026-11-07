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
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;
using WaveDudeNext.Runs;
using WaveDudeNext.Model;
using WaveDudeNext.Parts;

namespace WaveDudeNext.Avalonia.Rendering;

/// <summary>Decodes scene images to straight-alpha BGRA, preserving the channels used by the solver.</summary>
public static class SceneImageLoader
{
    /// <summary>Combines an image or blank canvas with the project's optical parts, as in the WPF loader.</summary>
    public static ScenePixels? Compose(Project project, string? projectFolder)
    {
        ScenePixels? image = null;
        if (project.Scene.Mode == SceneMode.Image)
        {
            if (project.Scene.ImageData is { } bytes)
                image = Load(bytes);
            else
            {
                string path = project.Scene.Image;
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidDataException("The scene uses an image, but no image is loaded.");
                if (!Path.IsPathRooted(path))
                    path = Path.GetFullPath(path, projectFolder ?? Environment.CurrentDirectory);
                image = Load(path);
            }
        }
        return PartPainter.Paint(image, project.Scene.Width, project.Scene.Height,
            project.Scene.RefractiveIndex, project.Parts);
    }

    public static ScenePixels Load(string path) => Load(File.ReadAllBytes(path));

    public static ScenePixels Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidDataException("The file is not a supported image.");
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        int stride = checked(info.Width * 4);
        var bgra = new byte[checked(stride * info.Height)];
        var handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
        try
        {
            if (codec.GetPixels(info, handle.AddrOfPinnedObject()) != SKCodecResult.Success)
                throw new InvalidDataException("The image could not be converted to BGRA pixels.");
        }
        finally
        {
            handle.Free();
        }
        return new ScenePixels(info.Width, info.Height, stride, bgra);
    }
}
