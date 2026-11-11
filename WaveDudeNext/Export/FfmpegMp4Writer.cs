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

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using NLog;

namespace WaveDudeNext.Export;

/// <summary>
/// Streams rendered BGRA32 frames to FFmpeg's H.264 encoder and lets FFmpeg write the MP4 container. The process
/// reads raw frames from standard input; no temporary image sequence or managed FFmpeg package is needed. The
/// <c>libx264</c> encoder is required and must be available in the FFmpeg build on <c>PATH</c>. One caller thread
/// at a time; closing standard input finalizes the movie, including when an export was cancelled.
/// </summary>
public sealed class FfmpegMp4Writer : IFrameSink
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	private static readonly Lazy<bool> Available = new(DetectAvailability);

	private readonly Process _process;
	private readonly Stream _input;
	private readonly Task<string> _errorOutput;
	private readonly int _encodedWidth;
	private readonly int _encodedHeight;
	private readonly int _fps;
	private readonly int[]? _padded;
	private bool _closed;

	/// <summary>
	/// Starts FFmpeg for an H.264 MP4. The quality setting maps linearly to x264 CRF 35 (lowest) through 14 (highest).
	/// Odd dimensions are padded by repeating the final row or column, as in <see cref="Mp4Writer"/>.
	/// </summary>
	/// <param name="path">Output .mp4 path; an existing file is replaced.</param>
	/// <param name="width">Frame width in pixels.</param>
	/// <param name="height">Frame height in pixels.</param>
	/// <param name="framesPerSecond">Playback frame rate.</param>
	/// <param name="quality">1..100: the H.264 quality setting.</param>
	/// <exception cref="NotSupportedException">FFmpeg with libx264 is unavailable, or the picture is smaller than 16 × 16.</exception>
	/// <exception cref="IOException">FFmpeg could not be started or the output could not be prepared.</exception>
	public FfmpegMp4Writer(string path, int width, int height, int framesPerSecond, int quality)
	{
		if (!IsAvailable())
		{
			throw new NotSupportedException("MP4 export needs FFmpeg with the libx264 encoder available on PATH.");
		}
		Width = width;
		Height = height;
		Output = path;
		_fps = Math.Max(1, framesPerSecond);
		Quality = Math.Clamp(quality, 1, 100);
		_encodedWidth = (width + 1) & ~1;
		_encodedHeight = (height + 1) & ~1;
		if (_encodedWidth < 16 || _encodedHeight < 16)
		{
			throw new NotSupportedException("The picture is too small for H.264 video (16 × 16 at least).");
		}
		_ = checked(_encodedWidth * _encodedHeight);
		if (_encodedWidth != width || _encodedHeight != height)
		{
			_padded = new int[_encodedWidth * _encodedHeight];
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}

		var start = new ProcessStartInfo("ffmpeg")
		{
			UseShellExecute = false,
			RedirectStandardInput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		foreach (string argument in new[]
		{
			"-hide_banner", "-loglevel", "error", "-nostats", "-y",
			"-f", "rawvideo", "-pixel_format", "bgra",
			"-video_size", _encodedWidth.ToString(CultureInfo.InvariantCulture) + "x" + _encodedHeight.ToString(CultureInfo.InvariantCulture),
			"-framerate", _fps.ToString(CultureInfo.InvariantCulture), "-i", "pipe:0",
			"-an", "-c:v", "libx264", "-preset", "medium", "-crf", Crf.ToString(CultureInfo.InvariantCulture),
			"-pix_fmt", "yuv420p", "-tag:v", "avc1", "-movflags", "+faststart", path,
		})
		{
			start.ArgumentList.Add(argument);
		}
		try
		{
			_process = Process.Start(start) ?? throw new IOException("FFmpeg did not start.");
		}
		catch (Win32Exception ex)
		{
			throw new IOException("Could not start FFmpeg. Install it and make sure ffmpeg is on PATH.", ex);
		}
		_input = _process.StandardInput.BaseStream;
		_errorOutput = _process.StandardError.ReadToEndAsync();
		Logger.Debug("MP4: FFmpeg/libx264, {Width} × {Height}, {Fps} fps, CRF {Crf}", _encodedWidth, _encodedHeight, _fps, Crf);
	}

	/// <summary>Whether FFmpeg and its libx264 encoder can be started from PATH.</summary>
	public static bool IsAvailable() => Available.Value;

	/// <summary>Width of each source frame in pixels.</summary>
	public int Width { get; }

	/// <summary>Height of each source frame in pixels.</summary>
	public int Height { get; }

	/// <summary>Frames successfully sent to FFmpeg.</summary>
	public int FrameCount { get; private set; }

	/// <summary>Output file path.</summary>
	public string Output { get; }

	/// <summary>Quality setting, from 1 to 100.</summary>
	public int Quality { get; }

	/// <summary>The x264 constant-rate-factor value selected for <see cref="Quality"/>.</summary>
	public int Crf => (int)Math.Round(35.0 - 21.0 * (Quality - 1) / 99.0, MidpointRounding.AwayFromZero);

	/// <inheritdoc/>
	public void Add(ReadOnlySpan<int> pixels)
	{
		if (_closed)
		{
			throw new InvalidOperationException("The video file is already closed.");
		}
		if (pixels.Length < Width * Height)
		{
			throw new ArgumentException("The frame is smaller than the video.", nameof(pixels));
		}
		if (_process.HasExited)
		{
			throw EncoderFailure(new IOException("FFmpeg exited before the next frame was written."));
		}

		ReadOnlySpan<int> frame = pixels[..(Width * Height)];
		if (_padded != null)
		{
			for (int y = 0; y < _encodedHeight; y++)
			{
				ReadOnlySpan<int> row = pixels.Slice(Math.Min(y, Height - 1) * Width, Width);
				Span<int> target = _padded.AsSpan(y * _encodedWidth, _encodedWidth);
				row.CopyTo(target);
				if (_encodedWidth > Width)
				{
					target[Width] = row[^1];
				}
			}
			frame = _padded;
		}
		try
		{
			// 0xAARRGGBB ints are BGRA in memory on the little-endian systems supported by the app.
			_input.Write(MemoryMarshal.AsBytes(frame));
			FrameCount++;
		}
		catch (IOException ex)
		{
			throw EncoderFailure(ex);
		}
	}

	/// <inheritdoc/>
	public void Close()
	{
		if (_closed)
		{
			return;
		}
		_closed = true;
		try
		{
			_input.Dispose();
			_process.WaitForExit();
			string diagnostics = _errorOutput.GetAwaiter().GetResult().Trim();
			if (FrameCount == 0)
			{
				if (File.Exists(Output))
				{
					File.Delete(Output);
				}
				return;
			}
			if (_process.ExitCode != 0 || !File.Exists(Output) || new FileInfo(Output).Length == 0)
			{
				throw new IOException(EncoderMessage(_process.ExitCode, diagnostics));
			}
			Logger.Debug("MP4: FFmpeg wrote {Frames} frames to {Output}", FrameCount, Output);
		}
		finally
		{
			_process.Dispose();
		}
	}

	/// <inheritdoc/>
	public void Dispose() => Close();

	private IOException EncoderFailure(Exception inner)
	{
		try
		{
			_input.Dispose();
			_process.WaitForExit();
		}
		catch (InvalidOperationException)
		{
		}
		string diagnostics = _errorOutput.GetAwaiter().GetResult().Trim();
		return new IOException(EncoderMessage(_process.ExitCode, diagnostics), inner);
	}

	private static string EncoderMessage(int exitCode, string diagnostics) =>
		"FFmpeg could not finish the H.264 MP4 export (exit code " + exitCode.ToString(CultureInfo.InvariantCulture) + ")." +
		(diagnostics.Length > 0 ? " " + diagnostics : "");

	private static bool DetectAvailability()
	{
		try
		{
			var start = new ProcessStartInfo("ffmpeg")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			start.ArgumentList.Add("-hide_banner");
			start.ArgumentList.Add("-encoders");
			using Process process = Process.Start(start) ?? throw new IOException("FFmpeg did not start.");
			Task<string> output = process.StandardOutput.ReadToEndAsync();
			Task<string> errors = process.StandardError.ReadToEndAsync();
			if (!process.WaitForExit(5000))
			{
				process.Kill(entireProcessTree: true);
				process.WaitForExit();
				return false;
			}
			string encoders = output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult();
			bool found = process.ExitCode == 0 && encoders.Contains("libx264", StringComparison.Ordinal);
			if (!found)
			{
				Logger.Debug("FFmpeg is unavailable for MP4 export: the libx264 encoder was not listed.");
			}
			return found;
		}
		catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
		{
			Logger.Debug(ex, "FFmpeg is unavailable for MP4 export: {Message}", ex.Message);
			return false;
		}
	}
}
