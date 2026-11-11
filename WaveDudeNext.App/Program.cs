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
//   Mark <mark@pab-ai.com>, co-author of the AvaloniaUI port
//
// WaveDude Next is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version. It is distributed WITHOUT ANY WARRANTY. See the LICENSE.md file or
// <https://www.gnu.org/licenses/>.

using System.Reflection;
using Avalonia;

namespace WaveDudeNext.App;

/// <summary>The entry point: the logging first, then Avalonia with the desktop lifetime.</summary>
public static class Program
{
	/// <summary>The version shown in the title bar, the log and the About window: the assembly's three-part version from Directory.Build.props.</summary>
	public static string DisplayVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "";

	// Initialization code. Don't use any Avalonia, third-party APIs or any SynchronizationContext-reliant code
	// before AppMain is called: things aren't initialized yet and stuff might break.
	[STAThread]
	public static int Main(string[] args)
	{
		App.SetupLogging();
		return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	/// <summary>
	/// Avalonia configuration; also used by the visual designer. No font of its own (2026-11-09, the user's rule: the same
	/// font as the WPF head): the platform's default family is the text's, Segoe UI on Windows as the WPF head has it,
	/// the system's UI font elsewhere. The port had bundled Inter.
	/// </summary>
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.With(new X11PlatformOptions
			{
				UseDBusFilePicker = false
			})
			.LogToTrace();
}
