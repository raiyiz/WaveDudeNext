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

namespace WaveDudeNext.Export;

/// <summary>The export formats, in the order of the Save dialog's filter: the videos first, then the sequences (the user's order, 2026-10-19).</summary>
public enum ExportFormat
{
	/// <summary>An H.264 MP4 through Media Foundation on Windows or FFmpeg with libx264 elsewhere; the quality setting applies.</summary>
	Mp4,
	/// <summary>An MPEG-1 program stream of I-pictures (<c>.mpg</c>); the quality setting and the nearest standard frame rate apply.</summary>
	Mpeg1,
	/// <summary>An uncompressed AVI with OpenDML indexes; lossless, large.</summary>
	Avi,
	/// <summary>A QuickTime movie with the Animation codec: lossless RGB, run-length coded, what the editing suites open natively.</summary>
	Mov,
	/// <summary>A QuickTime movie with the PNG codec: lossless RGB, each frame a PNG, far smaller than the Animation codec on a wave picture; for Resolve, Premiere and Final Cut (2026-10-19). Shares the extension with <see cref="Mov"/>: the dialog's filter tells them apart.</summary>
	MovPng,
	/// <summary>A PNG per frame in a folder of its own; lossless.</summary>
	Png,
	/// <summary>A JPEG per frame in a folder of its own; the quality setting applies.</summary>
	Jpeg,
	/// <summary>
	/// An OpenEXR file per frame in a folder of its own: 16-bit floats, one channel, uncompressed; data, not a rendered
	/// picture (2026-10-18). The field, and the intensity as I: a run that stores it so is made first (the preview stores its
	/// square root, and squaring that back would lose a bit). What a scripted <c>.exr</c> means.
	/// </summary>
	Exr,
	/// <summary>
	/// An OpenEXR file per frame like <see cref="Exr"/>, the intensity as its square root, as the preview stores it
	/// (2026-10-23): the dark end at half's full precision, and no extra run after a 16-bit preview. The field is the same.
	/// Shares the extension with <see cref="Exr"/>: the dialog's filter tells them apart.
	/// </summary>
	ExrRoot,
}

/// <summary>What is known about each <see cref="ExportFormat"/>: extensions, whether it is a sequence, how to open its sink.</summary>
public static class ExportFormats
{
	private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// The preferred JPEG encoder: given the frame size and the quality 1..100, a writer of one packed BGRA32
	/// frame to a stream. On Windows the WIC codec (<see cref="WicJpegEncoder.Encoder"/>, 4:4:4 natively),
	/// elsewhere null, which means the in-house <see cref="JpegEncoder"/>; when the preferred one throws, the
	/// sink logs it once and goes on with the in-house encoder. The harness swaps it to prove that fallback and
	/// to time the in-house encoder in a job (2026-10-18: until then the application installed WIC through a
	/// public hook, which the library no longer needs now that it holds the WIC encoder itself).
	/// </summary>
	internal static Func<int, int, int, Action<Stream, ReadOnlyMemory<int>>>? PreferredJpeg { get; set; } = DefaultPreferredJpeg();

	private static Func<int, int, int, Action<Stream, ReadOnlyMemory<int>>>? DefaultPreferredJpeg()
	{
		if (OperatingSystem.IsWindows())
		{
			return WicJpegEncoder.Encoder;
		}
		return null;
	}

	/// <summary>
	/// Frames of a sequence encoded at once: half the cores for encoders that are single-threaded per picture
	/// (PNG's zlib, the WIC codec), one for the in-house JPEG encoder, which already uses every core itself.
	/// </summary>
	public static int SequenceWorkers(bool encoderIsParallel) => encoderIsParallel ? 1 : Math.Clamp(Environment.ProcessorCount / 2, 1, 8);

	/// <summary>
	/// The JPEG writer for a size and quality: the preferred encoder with the in-house fallback (the in-house
	/// encoder is only built when needed), or the in-house one. One writer per thread: neither is shareable.
	/// </summary>
	private static Action<Stream, ReadOnlyMemory<int>> JpegWriter(int width, int height, int quality)
	{
		JpegEncoder? inHouse = null;
		Action<Stream, ReadOnlyMemory<int>> InHouse() => (inHouse ??= new JpegEncoder(width, height, quality)).Write;
		if (PreferredJpeg is not { } make)
		{
			return InHouse();
		}
		Action<Stream, ReadOnlyMemory<int>> preferred;
		try
		{
			preferred = make(width, height, quality);
		}
		catch (Exception ex)
		{
			Logger.Warn(ex, "The preferred JPEG encoder is not available ({Message}); using the in-house encoder", ex.Message);
			return InHouse();
		}
		bool fallen = false;
		return (stream, frame) =>
		{
			if (!fallen)
			{
				long start = stream.CanSeek ? stream.Position : 0;
				try
				{
					preferred(stream, frame);
					return;
				}
				catch (Exception ex)
				{
					fallen = true;
					Logger.Warn(ex, "The preferred JPEG encoder failed ({Message}); the in-house encoder takes over", ex.Message);
					if (stream.CanSeek)
					{
						stream.SetLength(start);
						stream.Position = start;
					}
				}
			}
			InHouse()(stream, frame);
		};
	}

	/// <summary>The formats in filter order: the videos, then the sequences.</summary>
	public static readonly ExportFormat[] All = [ExportFormat.Mp4, ExportFormat.Mpeg1, ExportFormat.Avi, ExportFormat.Mov, ExportFormat.MovPng, ExportFormat.Png, ExportFormat.Jpeg, ExportFormat.Exr, ExportFormat.ExrRoot];

	/// <summary>The formats a still can take, in the order of the still's filter: the sequence formats.</summary>
	public static readonly ExportFormat[] Stills = [ExportFormat.Png, ExportFormat.Jpeg, ExportFormat.Exr, ExportFormat.ExrRoot];

	/// <summary>The file extensions a format accepts, with their dots, the first the one it writes; both MOV formats take <c>.mov</c>, both OpenEXR ones <c>.exr</c>.</summary>
	public static string[] Extensions(ExportFormat format) => format switch
	{
		ExportFormat.Png => [".png"],
		ExportFormat.Jpeg => [".jpg", ".jpeg"],
		ExportFormat.Exr or ExportFormat.ExrRoot => [".exr"],
		ExportFormat.Mpeg1 => [".mpg", ".mpeg", ".m1v"],
		ExportFormat.Avi => [".avi"],
		ExportFormat.Mp4 => [".mp4", ".m4v"],
		ExportFormat.Mov or ExportFormat.MovPng => [".mov", ".qt"],
		_ => throw new ArgumentOutOfRangeException(nameof(format)),
	};

	/// <summary>The file extension a format writes, with its dot.</summary>
	public static string Extension(ExportFormat format) => Extensions(format)[0];

	/// <summary>Whether the path's extension is one the format accepts (case does not matter).</summary>
	public static bool Accepts(ExportFormat format, string path)
	{
		string extension = Path.GetExtension(path);
		foreach (string known in Extensions(format))
		{
			if (string.Equals(known, extension, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// The first format in filter order that accepts the path's extension, or null. The extension alone cannot tell
	/// the two MOV formats apart (a <c>.mov</c> means the Animation codec here) nor the two OpenEXR ones (an <c>.exr</c>
	/// holds the intensity as I): the dialog's filter decides the format, this serves a scripted path (2026-10-19).
	/// </summary>
	public static ExportFormat? FromPath(string path)
	{
		foreach (ExportFormat format in All)
		{
			if (Accepts(format, path))
			{
				return format;
			}
		}
		return null;
	}

	/// <summary>Whether the format writes one file per frame (these are also the formats a still can take).</summary>
	public static bool IsSequence(ExportFormat format) => format is ExportFormat.Png or ExportFormat.Jpeg or ExportFormat.Exr or ExportFormat.ExrRoot;

	/// <summary>Whether the format holds the stored values rather than a rendered picture (OpenEXR): colours, brightness, outline and boxes do not apply.</summary>
	public static bool IsData(ExportFormat format) => format is ExportFormat.Exr or ExportFormat.ExrRoot;

	/// <summary>
	/// What the run's 16-bit intensity must hold for an export of the intensity in this format: true for its square root
	/// (<see cref="ExportFormat.ExrRoot"/>), false for the intensity itself (<see cref="ExportFormat.Exr"/>), null when
	/// either serves (a picture is coloured from both alike).
	/// </summary>
	public static bool? IntensityRootFor(ExportFormat format) => format switch
	{
		ExportFormat.Exr => false,
		ExportFormat.ExrRoot => true,
		_ => null,
	};

	/// <summary>Whether the quality setting means anything to the format.</summary>
	public static bool UsesQuality(ExportFormat format) => format is ExportFormat.Jpeg or ExportFormat.Mpeg1 or ExportFormat.Mp4;

	/// <summary>Whether the format can be written here (MP4 needs Media Foundation on Windows or FFmpeg with libx264 elsewhere).</summary>
	public static bool IsAvailable(ExportFormat format) => format != ExportFormat.Mp4 || (OperatingSystem.IsWindows() ? Mp4Writer.IsAvailable() : FfmpegMp4Writer.IsAvailable());

	/// <summary>
	/// Where a sequence chosen as <c>C:\Temp\name.png</c> goes: the folder <c>C:\Temp\name</c>, holding
	/// <c>name_0000.png</c> and on (the user's rule, 2026-10-14).
	/// </summary>
	public static string SequenceFolder(string path) =>
		Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path));

	/// <summary>Opens a one-file sink for a still (PNG, JPEG or OpenEXR) at <paramref name="path"/>.</summary>
	/// <param name="format">PNG, JPEG or OpenEXR.</param>
	/// <param name="path">The file.</param>
	/// <param name="width">Frame width in pixels.</param>
	/// <param name="height">Frame height in pixels.</param>
	/// <param name="quality">1..100 for JPEG.</param>
	/// <exception cref="NotSupportedException">A video format was asked for.</exception>
	public static IFrameSink OpenStill(ExportFormat format, string path, int width, int height, int quality) => format switch
	{
		ExportFormat.Png => new ImageFileSink(path, width, height, (stream, frame) => PngEncoder.Write(stream, frame.Span, width, height)),
		ExportFormat.Jpeg => new ImageFileSink(path, width, height, JpegWriter(width, height, quality)),
		ExportFormat.Exr or ExportFormat.ExrRoot => ExrSink.Still(path, width, height),
		_ => throw new NotSupportedException("A still is a PNG, a JPEG or an OpenEXR file."),
	};

	/// <summary>
	/// Opens the sink for a format at <paramref name="path"/> (for a sequence, the folder of <see cref="SequenceFolder"/>).
	/// </summary>
	/// <param name="format">The format.</param>
	/// <param name="path">The file the user chose; a sequence takes its folder and base name from it.</param>
	/// <param name="width">Frame width in pixels.</param>
	/// <param name="height">Frame height in pixels.</param>
	/// <param name="fps">Frame rate of a video.</param>
	/// <param name="quality">1..100 for the lossy formats.</param>
	/// <param name="plannedFrames">How many frames will come at most (the AVI sizes its indexes by it, a sequence the width of its frame numbers).</param>
	/// <param name="firstFrame">The timeline frame number of the first frame: a sequence's files are numbered from it (2026-10-19).</param>
	/// <exception cref="NotSupportedException">The format cannot carry the size, or MP4 is not available here.</exception>
	public static IFrameSink Open(ExportFormat format, string path, int width, int height, int fps, int quality, int plannedFrames, int firstFrame = 0)
	{
		switch (format)
		{
			case ExportFormat.Png:
				return new ImageSequenceSink(SequenceFolder(path), Path.GetFileNameWithoutExtension(path), ".png", width, height,
					() => (stream, frame) => PngEncoder.Write(stream, frame.Span, width, height), SequenceWorkers(encoderIsParallel: false), plannedFrames, firstFrame);
			case ExportFormat.Jpeg:
				return new ImageSequenceSink(SequenceFolder(path), Path.GetFileNameWithoutExtension(path), ".jpg", width, height,
					() => JpegWriter(width, height, quality), SequenceWorkers(encoderIsParallel: PreferredJpeg == null), plannedFrames, firstFrame);
			case ExportFormat.Exr:
			case ExportFormat.ExrRoot:
				return ExrSink.Sequence(SequenceFolder(path), Path.GetFileNameWithoutExtension(path), width, height, plannedFrames, firstFrame);
			case ExportFormat.Mpeg1:
				return new Mpeg1Writer(path, width, height, fps, quality);
			case ExportFormat.Avi:
				return new AviWriter(path, width, height, fps, plannedFrames);
			case ExportFormat.Mp4:
				return OperatingSystem.IsWindows()
					? new Mp4Writer(path, width, height, fps, quality)
					: new FfmpegMp4Writer(path, width, height, fps, quality);
			case ExportFormat.Mov:
				return new MovWriter(path, width, height, fps, plannedFrames);
			case ExportFormat.MovPng:
				return new MovWriter(path, width, height, fps, plannedFrames, MovCodec.Png);
			default:
				throw new ArgumentOutOfRangeException(nameof(format));
		}
	}
}
