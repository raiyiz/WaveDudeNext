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

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using WaveDudeNext.Export;
using WaveDudeNext.Model;
using WaveDudeNext.Probes;
using WaveDudeNext.Rendering;
using WaveDudeNext.Runs;

namespace LibraryCheck;

// The export formats and the export job: every writer produces a file that an independent decoder (the
// ffmpeg found on the PATH, skipped when there is none) reads back with the right frame count and, for
// the lossless ones, the same pixels; the PNG is also decoded in-house. The job renders a run's frames
// into two sinks, calls the decoration per frame and ends in the right state.
internal static class ExportChecks
{
	private static readonly string? Ffmpeg = FindFfmpeg();

	public static void Run(Action<bool, string> check)
	{
		Console.WriteLine("--- export: encoders" + (Ffmpeg == null ? " (no ffmpeg on the PATH: decoding checks skipped)" : " (decoded with " + Ffmpeg + ")"));
		string folder = Path.Combine(Path.GetTempPath(), "library_check-export-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);
		try
		{
			const int w = 70; // not a multiple of 16, not even: exercises the padding of every format
			const int h = 45;
			int[][] frames = [.. Enumerable.Range(0, 7).Select(i => TestFrame(w, h, i))];

			// PNG: decoded in-house (inflate + unfilter) and by ffmpeg
			byte[] png = PngEncoder.Encode(frames[0], w, h);
			check(png.Length > 8 && png[1] == (byte)'P' && png[2] == (byte)'N' && png[3] == (byte)'G', "png: signature");
			int[]? decoded = DecodePng(png, w, h);
			check(decoded != null && decoded.SequenceEqual(frames[0]), "png: in-house decode gives the frame back pixel for pixel");
			// the unfiltered variant (the PNG movie's, 2026-10-19): every row's filter byte 0, the same picture back
			using var unfilteredStream = new MemoryStream();
			PngEncoder.Write(unfilteredStream, frames[0], w, h, PngFilter.None);
			byte[] unfiltered = unfilteredStream.ToArray();
			int[]? unfilteredDecoded = DecodePng(unfiltered, w, h);
			check(unfilteredDecoded != null && unfilteredDecoded.SequenceEqual(frames[0]) && FilterBytes(unfiltered, w, h) is { } filters && filters.All(f => f == 0),
				"png unfiltered: every row's filter byte is 0 and the frame comes back pixel for pixel");
			string pngPath = Path.Combine(folder, "one.png");
			File.WriteAllBytes(pngPath, png);
			if (Ffmpeg != null)
			{
				byte[]? rgb = Decode(pngPath, w, h, out int count);
				check(rgb != null && count == 1 && Psnr(rgb, frames[0], w, h) >= 99, "png: ffmpeg decodes it, identical" + Mismatch(rgb, frames[0], w, h));
			}

			// probe CSV (2026-10-18): semicolons, a decimal point, a header row; a point probe one row per step, a line probe one
			// row per frame with the distances along the line as the column heads; the proposed file name drops what a name cannot hold
			var csvPoint = new Probe { Name = "P 1", Kind = ProbeKind.Point, Layer = ProbeLayer.Intensity, Measurement = ProbeMeasurement.IntegratedValue };
			var pointTrace = new ProbeTrace();
			pointTrace.Series.Add([0.5f, -1.25f, 3f]);
			var text = new StringWriter();
			int rows = ProbeCsv.Write(text, csvPoint, pointTrace, 2, 1);
			string[] lines = text.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
			check(rows == 3 && lines is ["step;integrated value", "0;0.5", "1;-1.25", "2;3"], "probe csv: a point probe gives step;value rows, decimal point, semicolons (" + string.Join(" | ", lines) + ")");
			var csvLine = new Probe { Name = "L", Kind = ProbeKind.Line };
			var lineTrace = new ProbeTrace();
			lineTrace.AddProfile([1f, 2f, 3f], 4.0);
			lineTrace.AddProfile([4f, 5.5f, 6f], 4.0);
			text = new StringWriter();
			rows = ProbeCsv.Write(text, csvLine, lineTrace, 2, 1);
			lines = text.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
			check(rows == 2 && lines is ["frame;step;0;2;4", "0;0;1;2;3", "1;2;4;5.5;6"], "probe csv: a line probe gives one row per frame, frame and step first, the distances along the line as column heads (" + string.Join(" | ", lines) + ")");
			check(ProbeCsv.FileName("ball", new Probe { Name = "A/B: focus" }) == "ball_A_B_ focus.csv" && ProbeCsv.FileName("ball", new Probe { Name = " " }) == "ball_probe.csv", "probe csv: the proposed name is <project>_<probe>.csv with invalid characters replaced");

			// JPEG: a baseline file ffmpeg decodes close to the original
			var jpeg = new JpegEncoder(w, h, 90);
			byte[] jpg = jpeg.Encode(frames[0]);
			check(jpg.Length > 4 && jpg[0] == 0xFF && jpg[1] == 0xD8 && jpg[^2] == 0xFF && jpg[^1] == 0xD9, "jpeg: SOI and EOI markers");
			string jpgPath = Path.Combine(folder, "one.jpg");
			File.WriteAllBytes(jpgPath, jpg);
			byte[] jpgLow = new JpegEncoder(w, h, 20).Encode(frames[0]);
			check(jpgLow.Length < jpg.Length, "jpeg: quality 20 is smaller than quality 90 (" + jpgLow.Length + " < " + jpg.Length + " bytes)");
			check(SamplingFactors(jpg) is [0x11, 0x11, 0x11], "jpeg: the frame header says 4:4:4, every component sampled 1 × 1");
			if (OperatingSystem.IsWindows())
			{
				// the app's path: WIC called natively with its subsampling option, which WPF's wrapper never set (4:2:0 at every quality, 2026-10-18)
				byte[] wic = WicEncode(frames[0], w, h, 90);
				check(wic.Length > 4 && wic[0] == 0xFF && wic[1] == 0xD8 && SamplingFactors(wic) is [0x11, 0x11, 0x11], "jpeg by WIC: a JPEG whose frame header says 4:4:4");
				using (var file = new MemoryStream())
				{
					file.Write([1, 2, 3]); // the sink's stream may stand past a start: the adapter's positions are relative to it
					new WicJpegEncoder(w, h, 90).Write(file, frames[0]);
					check(file.Length == 3 + wic.Length && file.ToArray().AsSpan(3).SequenceEqual(wic), "jpeg by WIC: written to a stream from its current position, byte for byte the same");
				}
				if (Ffmpeg != null)
				{
					string wicPath = Path.Combine(folder, "wic.jpg");
					File.WriteAllBytes(wicPath, wic);
					byte[]? rgb = Decode(wicPath, w, h, out int count);
					double psnr = rgb == null ? 0 : Psnr(rgb, frames[0], w, h);
					check(rgb != null && count == 1 && psnr > 30, "jpeg by WIC: ffmpeg decodes it at " + psnr.ToString("0.0", CultureInfo.InvariantCulture) + " dB (> 30)");
				}
			}
			if (Ffmpeg != null)
			{
				byte[]? rgb = Decode(jpgPath, w, h, out int count);
				double psnr = rgb == null ? 0 : Psnr(rgb, frames[0], w, h);
				check(rgb != null && count == 1 && psnr > 30, "jpeg: ffmpeg decodes it at " + psnr.ToString("0.0", CultureInfo.InvariantCulture) + " dB (> 30)");
			}

			// MPEG-1: the frame rate table, the quantiser mapping, a 7-picture program stream
			check(Mpeg1Writer.NearestFrameRateCode(60) == 8 && Mpeg1Writer.NearestFrameRateCode(29.97) == 4 && Mpeg1Writer.NearestFrameRateCode(240) == 8 && Mpeg1Writer.NearestFrameRateCode(1) == 1, "mpeg-1: nearest frame rate codes (60 → 8, 29.97 → 4, 240 → 8, 1 → 1)");
			check(Mpeg1Writer.QuantiserScaleFor(100) == 1 && Mpeg1Writer.QuantiserScaleFor(1) == 31 && Mpeg1Writer.QuantiserScaleFor(70) == 6 && Mpeg1Writer.QuantiserScaleFor(50) == 12, "mpeg-1: quantiser scale 1 at 100, 31 at 1, 6 at 70, 12 at 50");
			string mpgPath = Path.Combine(folder, "seven.mpg");
			using (var mpeg = new Mpeg1Writer(mpgPath, w, h, 30, 85))
			{
				check(mpeg.FrameRate == 30 && mpeg.QuantiserScale == 3, "mpeg-1: 30 fps, quantiser 3 at quality 85");
				foreach (int[] frame in frames)
				{
					mpeg.Add(frame);
				}
				check(mpeg.FrameCount == 7, "mpeg-1: 7 pictures taken");
			}
			byte[] mpg = File.ReadAllBytes(mpgPath);
			check(mpg.Length > 100 && mpg[0] == 0 && mpg[1] == 0 && mpg[2] == 1 && mpg[3] == 0xBA && mpg[^4] == 0 && mpg[^3] == 0 && mpg[^2] == 1 && mpg[^1] == 0xB9, "mpeg-1: starts with a pack header, ends with the program end code");
			check(CountStartCodes(mpg, 0xB3) == 1 && CountStartCodes(mpg, 0x00) == 7 && CountStartCodes(mpg, 0xB7) == 1, "mpeg-1: one sequence header, seven picture headers, one sequence end");
			if (Ffmpeg != null)
			{
				byte[]? rgb = Decode(mpgPath, w, h, out int count);
				double worst = 99;
				if (rgb != null && count == 7)
				{
					for (int i = 0; i < 7; i++)
					{
						worst = Math.Min(worst, Psnr(rgb.AsSpan(i * w * h * 3, w * h * 3), frames[i], w, h));
					}
				}
				check(rgb != null && count == 7 && worst > 28, "mpeg-1: ffmpeg decodes 7 pictures, worst " + worst.ToString("0.0", CultureInfo.InvariantCulture) + " dB (> 28)" + (count != 7 ? ", got " + count : ""));
			}

			// AVI: three segments forced by a small limit, the structure walked, decoded pixel for pixel
			string aviPath = Path.Combine(folder, "seven.avi");
			int rowBytes = (w * 3 + 3) & ~3;
			long chunk = 8 + (long)rowBytes * h;
			using (var avi = new AviWriter(aviPath, w, h, 25, 7, segmentLimit: 3 * chunk + 1200))
			{
				foreach (int[] frame in frames)
				{
					avi.Add(frame);
				}
				check(avi.SegmentCount >= 2, "avi: the small limit split the frames over " + avi.SegmentCount + " segments");
				check(avi.SizeOf(7) > 7 * chunk, "avi: predicted size covers the frames");
			}
			long aviSize = new FileInfo(aviPath).Length;
			byte[] avi1 = File.ReadAllBytes(aviPath);
			check(Fourcc(avi1, 0) == "RIFF" && Fourcc(avi1, 8) == "AVI " && Fourcc(avi1, 12) == "LIST" && Fourcc(avi1, 20) == "hdrl" && Fourcc(avi1, 24) == "avih", "avi: RIFF AVI, LIST hdrl, avih");
			AviWalk walk = WalkAvi(avi1);
			check(walk.Segments >= 2 && walk.Frames == 7 && walk.TotalInDmlh == 7 && walk.Idx1Entries == walk.FramesInFirst && walk.StandardIndexes == walk.Segments && walk.SuperEntries == walk.Segments,
				"avi: " + walk.Segments + " segments hold 7 frames, dmlh says 7, idx1 covers the first segment's " + walk.FramesInFirst + ", one ix00 per segment, the super index lists them");
			check(walk.EndsAtFileEnd && walk.FirstRiffSize == walk.FirstSegmentBytes - 8, "avi: segment sizes add up to the file (" + aviSize + " bytes)");
			if (Ffmpeg != null)
			{
				byte[]? rgb = Decode(aviPath, w, h, out int count);
				bool exact = rgb != null && count == 7;
				for (int i = 0; exact && i < 7; i++)
				{
					exact = Psnr(rgb!.AsSpan(i * w * h * 3, w * h * 3), frames[i], w, h) >= 99;
				}
				check(exact, "avi: ffmpeg decodes 7 frames from all segments, identical" + (rgb == null ? "" : ", got " + count));
			}
			// a writer closed after one frame is a valid one-frame file
			string shortPath = Path.Combine(folder, "short.avi");
			using (var avi = new AviWriter(shortPath, w, h, 25, 7))
			{
				avi.Add(frames[3]);
			}
			AviWalk shortWalk = WalkAvi(File.ReadAllBytes(shortPath));
			check(shortWalk.Segments == 1 && shortWalk.Frames == 1 && shortWalk.TotalInDmlh == 1 && shortWalk.EndsAtFileEnd, "avi: cancelled after one frame is a valid one-frame file");

			// MOV with the Animation codec: the atoms, ffmpeg's qtrle decoder giving the frames back exactly, a run of one colour compressing
			string movPath = Path.Combine(folder, "seven.mov");
			bool moovFirst;
			using (var mov = new MovWriter(movPath, w, h, 25, 7))
			{
				foreach (int[] frame in frames)
				{
					mov.Add(frame);
				}
				check(mov.FrameCount == 7 && mov.FrameBytes < 7L * w * h * 3, "mov: 7 frames taken, smaller than raw (" + mov.FrameBytes + " of " + 7L * w * h * 3 + " bytes)");
				mov.Close();
				moovFirst = mov.MoovFirst;
			}
			byte[] movBytes = File.ReadAllBytes(movPath);
			int moovAt = IndexOf(movBytes, "moov"u8, 0, movBytes.Length);
			int mdatAt = IndexOf(movBytes, "mdat"u8, 0, movBytes.Length);
			check(Encoding.ASCII.GetString(movBytes, 4, 4) == "ftyp" && Encoding.ASCII.GetString(movBytes, 8, 4) == "qt  " && moovFirst && moovAt > 0 && moovAt < mdatAt
				&& IndexOf(movBytes, "free"u8, moovAt, mdatAt) > moovAt && IndexOf(movBytes, "rle "u8, 0, movBytes.Length) > 0 && IndexOf(movBytes, "co64"u8, 0, movBytes.Length) > 0,
				"mov: ftyp qt, the moov before the mdat with a free atom filling the reservation, an rle sample description, 64-bit offsets");
			// a writer told nothing about the count has no reservation and puts the moov at the end, still a valid file
			using (var trailing = new MovWriter(Path.Combine(folder, "trailing.mov"), w, h, 25))
			{
				foreach (int[] frame in frames)
				{
					trailing.Add(frame);
				}
				trailing.Close();
				byte[] t = File.ReadAllBytes(Path.Combine(folder, "trailing.mov"));
				check(!trailing.MoovFirst && IndexOf(t, "moov"u8, 0, t.Length) > IndexOf(t, "mdat"u8, 0, t.Length), "mov: with no planned count the moov goes at the end");
			}
			if (Ffmpeg != null)
			{
				byte[]? rgb = Decode(movPath, w, h, out int count);
				bool exact = rgb != null && count == 7;
				for (int i = 0; exact && i < 7; i++)
				{
					exact = Psnr(rgb!.AsSpan(i * w * h * 3, w * h * 3), frames[i], w, h) >= 99;
				}
				check(exact, "mov: ffmpeg decodes 7 frames, identical" + (rgb == null ? "" : ", got " + count));
			}
			var flat = new int[w * h];
			Array.Fill(flat, unchecked((int)0xFF123456));
			var scratch = new byte[6 + h * (w * 3 + w / 127 + 4)];
			using (var mov = new MovWriter(Path.Combine(folder, "flat.mov"), w, h, 25))
			{
				int flatBytes = mov.Encode(flat, scratch);
				check(flatBytes < h * 8 + 6, "mov: a flat frame codes to " + flatBytes + " bytes (under " + (h * 8 + 6) + ")");
			}
			// MOV with the PNG codec (2026-10-19, for the editing suites): a 'png ' sample description, each sample a whole PNG
			// file that decodes to its frame, smaller than the Animation codec on these frames, ffmpeg giving the frames back exactly
			string pngMovPath = Path.Combine(folder, "png.mov");
			long animationBytes;
			using (var mov = new MovWriter(movPath + ".again.mov", w, h, 25, 7))
			{
				foreach (int[] frame in frames)
				{
					mov.Add(frame);
				}
				animationBytes = mov.FrameBytes;
			}
			using (var mov = new MovWriter(pngMovPath, w, h, 25, 7, MovCodec.Png))
			{
				foreach (int[] frame in frames)
				{
					mov.Add(frame);
				}
				mov.Close();
				check(mov.FrameCount == 7 && mov.MoovFirst && mov.Codec == MovCodec.Png, "mov png: 7 frames taken, the moov leading");
				check(mov.FrameBytes < animationBytes, "mov png: smaller than the Animation codec (" + mov.FrameBytes + " against " + animationBytes + " bytes)");
			}
			byte[] pngMov = File.ReadAllBytes(pngMovPath);
			ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
			int firstSample = IndexOf(pngMov, signature, 0, pngMov.Length);
			int secondSample = firstSample < 0 ? -1 : IndexOf(pngMov, signature, firstSample + 8, pngMov.Length);
			check(IndexOf(pngMov, "png "u8, 0, pngMov.Length) > 0 && IndexOf(pngMov, "rle "u8, 0, pngMov.Length) < 0 && firstSample > 0 && secondSample > firstSample,
				"mov png: a 'png ' sample description and PNG files as samples");
			int[]? firstFrame = secondSample > firstSample ? DecodePng(pngMov[firstSample..secondSample], w, h) : null;
			bool firstExact = firstFrame != null;
			for (int i = 0; firstExact && i < w * h; i++)
			{
				firstExact = (firstFrame![i] & 0xFFFFFF) == (frames[0][i] & 0xFFFFFF);
			}
			check(firstExact, "mov png: the first sample decodes to the first frame exactly");
			if (Ffmpeg != null)
			{
				byte[]? rgb = Decode(pngMovPath, w, h, out int count);
				bool exact = rgb != null && count == 7;
				for (int i = 0; exact && i < 7; i++)
				{
					exact = Psnr(rgb!.AsSpan(i * w * h * 3, w * h * 3), frames[i], w, h) >= 99;
				}
				check(exact, "mov png: ffmpeg decodes 7 frames, identical" + (rgb == null ? "" : ", got " + count));
			}
			// MP4: Media Foundation, when it is there
			if (OperatingSystem.IsWindows() && ExportFormats.IsAvailable(ExportFormat.Mp4))
			{
				string mp4Path = Path.Combine(folder, "seven.mp4");
				try
				{
					using (var mp4 = new Mp4Writer(mp4Path, w, h, 30, 80))
					{
						foreach (int[] frame in frames)
						{
							mp4.Add(frame);
						}
						Console.WriteLine("  mp4: bitrate " + mp4.Bitrate + " bit/s" + (mp4.UsedBitrateOnly ? ", quality parameters refused" : ", quality parameters accepted"));
					}
					byte[] mp4Bytes = File.ReadAllBytes(mp4Path);
					check(mp4Bytes.Length > 100 && Encoding.ASCII.GetString(mp4Bytes, 4, 4) == "ftyp", "mp4: ftyp box, " + mp4Bytes.Length + " bytes");
					if (Ffmpeg != null)
					{
						byte[]? rgb = Decode(mp4Path, w, h, out int count);
						double psnr = rgb == null || count < 1 ? 0 : Psnr(rgb.AsSpan(0, w * h * 3), frames[0], w, h);
						check(rgb != null && count == 7 && psnr > 25, "mp4: ffmpeg decodes 7 frames, the first at " + psnr.ToString("0.0", CultureInfo.InvariantCulture) + " dB (> 25)" + (rgb == null ? "" : ", got " + count));
					}
				}
				catch (Exception ex)
				{
					check(false, "mp4: " + ex.Message);
				}
			}
			else
			{
				Console.WriteLine("  skip Media Foundation MP4: the Windows encoder is not available");
			}


			// FFmpeg/libx264: raw BGRA frames streamed directly to an MP4, including odd-size edge padding
			if (FfmpegMp4Writer.IsAvailable())
			{
				string ffmpegMp4Path = Path.Combine(folder, "seven-ffmpeg.mp4");
				try
				{
					using (var mp4 = new FfmpegMp4Writer(ffmpegMp4Path, w, h, 30, 80))
					{
						foreach (int[] frame in frames)
						{
							mp4.Add(frame);
						}
						check(mp4.FrameCount == 7 && mp4.Crf is >= 14 and <= 35, "mp4 by ffmpeg: 7 frames sent, quality mapped to CRF " + mp4.Crf);
					}
					byte[] mp4Bytes = File.ReadAllBytes(ffmpegMp4Path);
					check(mp4Bytes.Length > 100 && Encoding.ASCII.GetString(mp4Bytes, 4, 4) == "ftyp", "mp4 by ffmpeg: ftyp box, " + mp4Bytes.Length + " bytes");
					if (Ffmpeg != null)
					{
						int encodedWidth = (w + 1) & ~1;
						int encodedHeight = (h + 1) & ~1;
						byte[]? rgb = Decode(ffmpegMp4Path, encodedWidth, encodedHeight, out int count);
						int[] expected = PadEven(frames[0], w, h);
						double psnr = rgb == null || count < 1 ? 0 : Psnr(rgb.AsSpan(0, encodedWidth * encodedHeight * 3), expected, encodedWidth, encodedHeight);
						check(rgb != null && count == 7 && psnr > 25, "mp4 by ffmpeg: ffmpeg decodes 7 padded frames, first at " + psnr.ToString("0.0", CultureInfo.InvariantCulture) + " dB (> 25)" + (rgb == null ? "" : ", got " + count));
					}
				}
				catch (Exception ex)
				{
					check(false, "mp4 by ffmpeg: " + ex.Message);
				}
			}
			else
			{
				Console.WriteLine("  skip ffmpeg mp4: ffmpeg with libx264 not available");
			}

			// formats
			check(ExportFormats.FromPath(@"C:\x\a.JPEG") == ExportFormat.Jpeg && ExportFormats.FromPath("a.mpeg") == ExportFormat.Mpeg1 && ExportFormats.FromPath("a.txt") == null, "formats: extensions map, unknown is null");
			check(ExportFormats.SequenceFolder(@"C:\Temp\chosenname.png") == @"C:\Temp\chosenname", "formats: a sequence chosen as C:\\Temp\\chosenname.png goes to C:\\Temp\\chosenname");
			using (IFrameSink sink = ExportFormats.Open(ExportFormat.Png, Path.Combine(folder, "seq.png"), w, h, 30, 70, 3))
			{
				sink.Add(frames[1]);
				sink.Add(frames[2]);
			}
			check(File.Exists(Path.Combine(folder, "seq", "seq_0000.png")) && File.Exists(Path.Combine(folder, "seq", "seq_0001.png")) && !File.Exists(Path.Combine(folder, "seq", "seq_0002.png")), "formats: the PNG sequence sink writes seq\\seq_0000.png and seq_0001.png");
			check(DecodePng(File.ReadAllBytes(Path.Combine(folder, "seq", "seq_0001.png")), w, h)!.SequenceEqual(frames[2]), "formats: the second file holds the second frame");
			string stillPath = Path.Combine(folder, "still.png");
			using (IFrameSink still = ExportFormats.OpenStill(ExportFormat.Png, stillPath, w, h, 70))
			{
				still.Add(frames[4]);
				bool refused = false;
				try
				{
					still.Add(frames[5]);
				}
				catch (InvalidOperationException)
				{
					refused = true;
				}
				check(still.FrameCount == 1 && refused, "formats: a still takes one frame and refuses a second");
			}
			check(DecodePng(File.ReadAllBytes(stillPath), w, h)!.SequenceEqual(frames[4]), "formats: the still is the frame");
			// OpenEXR (2026-10-18): the stored halves as a one-channel scanline file, parsed in-house and decoded by ffmpeg
			var halves = new Half[w * h];
			for (int i = 0; i < halves.Length; i++)
			{
				halves[i] = (Half)(Math.Sin(i * 0.013) * (i % 7 == 0 ? -1.5 : 0.8)); // negatives and a value beyond 1 included
			}
			using (IFrameSink exr = ExportFormats.Open(ExportFormat.Exr, Path.Combine(folder, "exr.exr"), w, h, 30, 70, 3))
			{
				bool pixelsRefused = false;
				try
				{
					exr.Add(frames[0]);
				}
				catch (NotSupportedException)
				{
					pixelsRefused = true;
				}
				exr.Add(new Half[w * h]);
				exr.Add(halves);
				check(exr.WantsHalves && pixelsRefused && exr.FrameCount == 2 && !ExportFormats.Open(ExportFormat.Png, Path.Combine(folder, "p.png"), w, h, 30, 70, 1).WantsHalves,
					"formats: the OpenEXR sink wants halves and refuses pixels; the PNG sink does not");
			}
			string exrFile = Path.Combine(folder, "exr", "exr_0001.exr");
			string exrNote = "";
			Half[]? back = File.Exists(exrFile) ? ReadExr(File.ReadAllBytes(exrFile), w, h, out exrNote) : null;
			check(back != null && MemoryMarshal.AsBytes(back.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(halves.AsSpan())),
				"formats: exr_0001.exr is a version 2 scanline file, channel Y half, uncompressed, with the halves bit for bit" + (back == null ? " (" + (File.Exists(exrFile) ? exrNote : "no file") + ")" : ""));
			if (Ffmpeg != null)
			{
				// decoded to 16-bit grey, the one form every ffmpeg since 2018 gives for a one-channel EXR: value × 65536,
				// clipped to 0..65535 (the negatives and the 1.5 land on the limits); so the values and their places are checked
				ushort[]? decodedExr = DecodeGray16(exrFile, w, h);
				bool same = decodedExr != null;
				for (int i = 0; same && i < halves.Length; i++)
				{
					int expected = (int)Math.Clamp(Math.Round((float)halves[i] * 65536f), 0, 65535);
					same = Math.Abs(decodedExr![i] - expected) <= 2;
				}
				check(same, "formats: ffmpeg decodes the OpenEXR file to the same values (16-bit grey, value × 65536, clipped)");
			}
			string exrStill = Path.Combine(folder, "still.exr");
			using (IFrameSink still = ExportFormats.OpenStill(ExportFormat.Exr, exrStill, w, h, 70))
			{
				still.Add(halves);
			}
			check(ReadExr(File.ReadAllBytes(exrStill), w, h, out _) is { } stillBack && MemoryMarshal.AsBytes(stillBack.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(halves.AsSpan())), "formats: an OpenEXR still holds the frame");
			// the two OpenEXR entries (2026-10-23): the intensity as I or as its square root, one extension, the dialog's
			// filter telling them apart; a scripted .exr is I
			using (IFrameSink rootSink = ExportFormats.Open(ExportFormat.ExrRoot, Path.Combine(folder, "root.exr"), w, h, 30, 70, 1))
			{
				check(rootSink.WantsHalves && ExportFormats.IsData(ExportFormat.ExrRoot) && ExportFormats.IsSequence(ExportFormat.ExrRoot) && ExportFormats.Extension(ExportFormat.ExrRoot) == ".exr"
					&& ExportFormats.FromPath("data.exr") == ExportFormat.Exr && ExportFormats.IntensityRootFor(ExportFormat.Exr) == false && ExportFormats.IntensityRootFor(ExportFormat.ExrRoot) == true
					&& ExportFormats.IntensityRootFor(ExportFormat.Png) == null && ExportFormats.Stills.Contains(ExportFormat.ExrRoot) && ExportFormats.All[^1] == ExportFormat.ExrRoot,
					"formats: OpenEXR of the intensity as I (a scripted .exr) or as its square root, both data, both .exr; pictures take either");
			}
			bool videoRefused = false;
			try
			{
				ExportFormats.OpenStill(ExportFormat.Avi, Path.Combine(folder, "still.avi"), w, h, 70);
			}
			catch (NotSupportedException)
			{
				videoRefused = true;
			}
			check(videoRefused, "formats: a still is never a video");
			// the author's marker strokes: a line probe's line and crosses, black radius 2 under white radius 1, clipped
			var marked = new int[w * h];
			var lineProbe = new Probe { Kind = ProbeKind.Line, AX = 10, AY = 10, BX = 60, BY = 30 };
			ProbeMarkers.DrawStrokes(marked, w, h, lineProbe);
			int white = unchecked((int)0xFFFFFFFF);
			int black = unchecked((int)0xFF000000);
			check(marked[10 * w + 10] == white && marked[30 * w + 60] == white && marked[20 * w + 35] == white, "markers: the crosses' centres and the line's middle are white");
			var pointProbe = new Probe { Kind = ProbeKind.Point, AX = 40, AY = 40 };
			ProbeMarkers.DrawStrokes(marked, w, h, pointProbe);
			check(marked[42 * w + 42] == black && marked[42 * w + 38] == black && marked[40 * w + 44] == white && marked[(40 - ProbeMarkers.ArmLength(h) - 3) * w + 40] != black, "markers: the black stroke lies around the white one and ends with the arm");
			check(ProbeMarkers.ArmLength(1080) == 9 && ProbeMarkers.ArmLength(100) == 6 && Math.Abs(ProbeMarkers.FontSize(1080) - 18f) < 0.01f && ProbeMarkers.LabelOrigin(100, 100, 1080) == (111, 66), "markers: the author's arm length, font size and label position at 1080p");
			var edgeProbe = new Probe { Kind = ProbeKind.Point, AX = 0, AY = 0 };
			ProbeMarkers.DrawStrokes(marked, w, h, edgeProbe);
			check(marked[0] == white && ProbeMarkers.Label(3, true, true) == "3B" && ProbeMarkers.Label(2, false, false) == "2", "markers: a cross at the corner is clipped, labels read 3B and 2");
			// the preferred JPEG encoder is used (on several workers), and one that throws on a frame hands that frame to the
			// in-house encoder; the frames after it come from whichever encoder their worker is left with, both valid
			int alternativeCalls = 0;
			int secondFrameMark = (frames[1][1] >> 16) & 0xFF;
			Func<int, int, int, Action<Stream, ReadOnlyMemory<int>>>? preferred = ExportFormats.PreferredJpeg;
			ExportFormats.PreferredJpeg = (fw, fh, q) => (stream, frame) =>
			{
				Interlocked.Increment(ref alternativeCalls);
				if (((frame.Span[1] >> 16) & 0xFF) == secondFrameMark)
				{
					stream.WriteByte(0x42); // half-written: the fallback must rewind
					throw new InvalidOperationException("the codec gave up");
				}
				PngEncoder.Write(stream, frame.Span, fw, fh); // recognisable: not a JPEG at all
			};
			using (IFrameSink alt = ExportFormats.Open(ExportFormat.Jpeg, Path.Combine(folder, "alt.jpg"), w, h, 30, 85, 3))
			{
				alt.Add(frames[0]);
				alt.Add(frames[1]);
				alt.Add(frames[2]);
			}
			ExportFormats.PreferredJpeg = preferred;
			byte[] alt0 = File.ReadAllBytes(Path.Combine(folder, "alt", "alt_0000.jpg"));
			byte[] alt1 = File.ReadAllBytes(Path.Combine(folder, "alt", "alt_0001.jpg"));
			byte[] alt2 = File.ReadAllBytes(Path.Combine(folder, "alt", "alt_0002.jpg"));
			check(alternativeCalls >= 2 && alt0[1] == (byte)'P' && alt1[0] == 0xFF && alt1[1] == 0xD8 && (alt2[1] == (byte)'P' || (alt2[0] == 0xFF && alt2[1] == 0xD8)),
				"formats: the preferred JPEG encoder is used; the frame it throws on is rewound and written by the in-house encoder");

			CheckJob(check, folder);
		}
		finally
		{
			try
			{
				Directory.Delete(folder, recursive: true);
			}
			catch (IOException)
			{
				// a file still open by a decoder; the temp folder is cleaned up another day
			}
		}
	}

	/// <summary>A run of 10 frames exported to a PNG sequence (field, decorated) and an AVI (intensity); then a cancelled job.</summary>
	private static void CheckJob(Action<bool, string> check, string folder)
	{
		// the frame numbers' width follows the planned count: four digits up to 10000 frames, five beyond (2026-10-15)
		using (var narrow = new ImageSequenceSink(Path.Combine(folder, "narrow"), "f", ".png", 4, 4, () => (stream, px) => PngEncoder.Write(stream, px.Span, 4, 4), 1, 500))
		using (var wide = new ImageSequenceSink(Path.Combine(folder, "wide"), "f", ".png", 4, 4, () => (stream, px) => PngEncoder.Write(stream, px.Span, 4, 4), 1, 12000))
		{
			check(narrow.Digits == 4 && narrow.PathOf(7).EndsWith("f_0007.png", StringComparison.Ordinal) && wide.Digits == 5 && wide.PathOf(7).EndsWith("f_00007.png", StringComparison.Ordinal) && wide.PathOf(10000).EndsWith("f_10000.png", StringComparison.Ordinal),
				"sequence names: 4 digits for 500 frames, 5 for 12000, so they sort past frame 9999");
		}
		Console.WriteLine("--- export: job");
		var project = new Project();
		project.Scene.Width = 64;
		project.Scene.Height = 48;
		project.Simulation.Steps = 20;
		project.Simulation.VisualizeEvery = 2;
		Source source = project.AddSource();
		source.X = 20;
		source.Y = 24;
		source.Frequency = 120;
		RunSpec spec = RunSpec.Create(project, null, preferGpu: false);
		string storePath = Path.Combine(folder, "job" + FrameCache.Extension);
		using var store = FrameStore.Create(storePath, spec.Width, spec.Height, spec.Layers, spec.FrameCount, spec.VisualizeEvery);
		using var run = new SimulationRun(spec, store, null);
		run.Start();
		check(run.Completion.Wait(30000) && store.Count == 10, "job: a 10-frame run to export");

		ColorLut lut = ColorLut.Build(ColorSchemeStore.LoadBuiltIn(null).First(scheme => scheme.Name == Display.DefaultFieldColorScheme));
		var decorated = new List<int>();
		var fieldLayer = new ExportLayer(FrameLayer.Field, lut, 30f, null, 0, (frame, pixels) =>
		{
			decorated.Add(frame);
			pixels.Span[0] = unchecked((int)0xFFFF0000); // a red pixel at the top left, to be found in the file
		});
		var intensityLayer = new ExportLayer(FrameLayer.Intensity, ColorLut.Grey, 30f, null, 0, null);
		string seqPath = Path.Combine(folder, "field.png");
		string aviPath = Path.Combine(folder, "intensity.avi");
		string exrPath = Path.Combine(folder, "data.exr");
		var targets = new List<ExportTarget>
		{
			new(fieldLayer, ExportFormats.SequenceFolder(seqPath), () => ExportFormats.Open(ExportFormat.Png, seqPath, 64, 48, 30, 70, 6, 2)),
			new(intensityLayer, aviPath, () => ExportFormats.Open(ExportFormat.Avi, aviPath, 64, 48, 30, 70, 6)),
			// a data target beside the pictures: the job hands it the stored halves, nothing rendered or decorated
			new(new ExportLayer(FrameLayer.Field, lut, 30f, null, 0, (_, _) => throw new InvalidOperationException("a data target is never decorated")),
				ExportFormats.SequenceFolder(exrPath), () => ExportFormats.Open(ExportFormat.Exr, exrPath, 64, 48, 30, 70, 6, 2)),
		};
		var job = new ExportJob(store, 2, 7, targets, null);
		var states = new List<ExportState>();
		job.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(ExportJob.State))
			{
				states.Add(job.State);
			}
		};
		int finished = 0;
		job.Finished += (_, _) => finished++;
		job.Start();
		check(job.Completion.Wait(30000) && job.State == ExportState.Completed && finished == 1 && job.Error == null, "job: completes, Finished once");
		check(targets.All(target => target.Sink != null && target.Sink.Output == target.Output), "job: the sinks were opened by the job, where the targets said");
		check(states.SequenceEqual([ExportState.Running, ExportState.Completed]) && job.FramesDone == 6 && job.FrameCount == 6, "job: Running then Completed, 6 of 6 frames");
		check(decorated.SequenceEqual([2, 3, 4, 5, 6, 7]), "job: the decoration saw frames 2 to 7 in order");
		string[] files = Directory.GetFiles(Path.Combine(folder, "field"));
		check(files.Length == 6 && File.Exists(Path.Combine(folder, "field", "field_0002.png")) && File.Exists(Path.Combine(folder, "field", "field_0007.png")) && !File.Exists(Path.Combine(folder, "field", "field_0000.png")),
			"job: six PNGs numbered by timeline frame, field_0002 to field_0007");
		// the first file is frame 2 rendered with the same LUT and gain, plus the decoration
		var values = new Half[64 * 48];
		var expected = new int[64 * 48];
		store.CopyLayer(2, FrameLayer.Field, values);
		FrameRenderer.Render(values, 64, 48, lut, SampleKind.Field, 30f, expected);
		expected[0] = unchecked((int)0xFFFF0000);
		int[]? first = DecodePng(File.ReadAllBytes(Path.Combine(folder, "field", "field_0002.png")), 64, 48);
		check(first != null && first.SequenceEqual(expected), "job: field_0002.png is frame 2 as the renderer colours it, with the decoration");
		AviWalk walk = WalkAvi(File.ReadAllBytes(aviPath));
		check(walk.Frames == 6 && walk.TotalInDmlh == 6 && walk.EndsAtFileEnd, "job: the intensity AVI holds 6 frames");
		string exrFirst = Path.Combine(folder, "data", "data_0002.exr");
		Half[]? exrValues = File.Exists(exrFirst) ? ReadExr(File.ReadAllBytes(exrFirst), 64, 48, out _) : null;
		check(Directory.GetFiles(Path.Combine(folder, "data")).Length == 6 && exrValues != null && MemoryMarshal.AsBytes(exrValues.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(values.AsSpan())),
			"job: data_0002.exr holds frame 2's stored halves bit for bit, undecorated, beside the pictures");

		// cancelled: a slow decoration lets the cancel land after the first frame
		using var started = new ManualResetEventSlim();
		var slowLayer = new ExportLayer(FrameLayer.Field, lut, 30f, null, 0, (_, _) =>
		{
			started.Set();
			Thread.Sleep(150);
		});
		string cancelPath = Path.Combine(folder, "cancel.avi");
		var cancelled = new ExportJob(store, 0, 9, [new ExportTarget(slowLayer, cancelPath, () => ExportFormats.Open(ExportFormat.Avi, cancelPath, 64, 48, 30, 70, 10))], null);
		cancelled.Start();
		started.Wait(10000);
		cancelled.Cancel();
		check(cancelled.Completion.Wait(10000) && cancelled.State == ExportState.Cancelled && cancelled.FramesDone >= 1 && cancelled.FramesDone < 10, "job: cancel ends it Cancelled after " + cancelled.FramesDone + " frames");
		AviWalk cancelWalk = WalkAvi(File.ReadAllBytes(cancelPath));
		check(cancelWalk.Frames == cancelled.FramesDone && cancelWalk.EndsAtFileEnd, "job: the cancelled AVI is a valid file of the frames written");

		// a frame beyond the store fails the job, the sinks still close
		string failPath = Path.Combine(folder, "fail.avi");
		var failing = new ExportJob(store, 8, 12, [new ExportTarget(intensityLayer, failPath, () => ExportFormats.Open(ExportFormat.Avi, failPath, 64, 48, 30, 70, 5))], null);
		failing.Start();
		check(failing.Completion.Wait(10000) && failing.State == ExportState.Failed && failing.Error is InvalidOperationException && failing.FramesDone == 2, "job: a frame beyond the store fails it after the 2 that exist");
		// a sink that cannot open fails the job before any frame, with the error kept
		var unopenable = new ExportJob(store, 0, 1, [new ExportTarget(intensityLayer, "nowhere", () => throw new IOException("no such drive"))], null);
		unopenable.Start();
		check(unopenable.Completion.Wait(10000) && unopenable.State == ExportState.Failed && unopenable.Error is IOException && unopenable.FramesDone == 0, "job: a sink that cannot open fails the job with its error");
		// an MP4 written by the job: the platform writer is created, fed and closed on the sink thread
		if (ExportFormats.IsAvailable(ExportFormat.Mp4))
		{
			string mp4Path = Path.Combine(folder, "job.mp4");
			var mp4Job = new ExportJob(store, 0, 9, [new ExportTarget(fieldLayer, mp4Path, () => ExportFormats.Open(ExportFormat.Mp4, mp4Path, 64, 48, 30, 70, 10))], null);
			mp4Job.Start();
			check(mp4Job.Completion.Wait(30000) && mp4Job.State == ExportState.Completed && mp4Job.FramesDone == 10 && new FileInfo(mp4Path).Length > 100, "job: an MP4 of 10 frames written on the job's thread" + (mp4Job.Error != null ? " (" + mp4Job.Error.Message + ")" : ""));
		}
	}

	/// <summary>Per-frame cost of every encoder at a given size, for `-- bench-export [w h]`.</summary>
	public static void Bench(int w, int h)
	{
		Console.WriteLine($"--- export bench {w} x {h}, {Environment.ProcessorCount} cores");
		int[] frame = TestFrame(w, h, 3);
		string folder = Path.Combine(Path.GetTempPath(), "library_check-bench-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);
		try
		{
			Time("png encode", () => PngEncoder.Encode(frame, w, h), 3);
			var jpeg = new JpegEncoder(w, h, 85);
			Time("jpeg encode (q 85)", () => jpeg.Encode(frame), 5);
			Console.WriteLine($"    phases: convert {jpeg.LastPhases.Convert:0.0} ms, rows {jpeg.LastPhases.Rows:0.0} ms; Vector128 accelerated: {System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated}, Vector256: {System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated}, Vector<float>.Count {System.Numerics.Vector<float>.Count}");
			var planes = new YuvPlanes(w, h, studioRange: false, subsampled: false);
			Time("  of which yuv conversion", () => planes.Convert(frame), 5);
			jpeg.ParallelRows = false;
			Time("jpeg encode, rows one after the other", () => jpeg.Encode(frame), 3);
			Console.WriteLine($"    phases: convert {jpeg.LastPhases.Convert:0.0} ms, rows {jpeg.LastPhases.Rows:0.0} ms");
			jpeg.ParallelRows = true;
			foreach (int threads in new[] { 1, 2, 4, Environment.ProcessorCount })
			{
				jpeg.MaxThreads = threads;
				Time($"jpeg encode on {threads} thread(s)", () => jpeg.Encode(frame), 3);
			}
			jpeg.MaxThreads = -1;
			// the WIC codec of Windows called natively at 4:4:4 (the app's path), timed and compared at the same quality
			if (OperatingSystem.IsWindows())
			{
				byte[] inHouse = jpeg.Encode(frame);
				byte[] wic = [];
				Time("jpeg encode by WIC (q 85)", () => wic = WicEncode(frame, w, h, 85), 5);
				File.WriteAllBytes(Path.Combine(folder, "wic.jpg"), wic);
				File.WriteAllBytes(Path.Combine(folder, "inhouse.jpg"), inHouse);
				double wicDb = 0;
				double inHouseDb = 0;
				if (Ffmpeg != null)
				{
					byte[]? rgb = Decode(Path.Combine(folder, "wic.jpg"), w, h, out int n);
					wicDb = rgb == null || n != 1 ? 0 : Psnr(rgb, frame, w, h);
					rgb = Decode(Path.Combine(folder, "inhouse.jpg"), w, h, out n);
					inHouseDb = rgb == null || n != 1 ? 0 : Psnr(rgb, frame, w, h);
				}
				Console.WriteLine($"    at quality 85: WIC {wic.Length / 1024} KB at {wicDb:0.0} dB, in-house {inHouse.Length / 1024} KB at {inHouseDb:0.0} dB");
				foreach (int q in new[] { 50, 70, 95 })
				{
					byte[] a = WicEncode(frame, w, h, q);
					byte[] b = new JpegEncoder(w, h, q).Encode(frame);
					Console.WriteLine($"    at quality {q}: WIC {a.Length / 1024} KB, in-house {b.Length / 1024} KB");
				}
			}
			// the per-block costs, single-threaded over as many blocks as a frame has
			int blocks = planes.MacroblockColumns * planes.MacroblockRows * (planes.Subsampled ? 6 : 12);
			var block = new float[64];
			var dct = new float[64];
			Time($"  {blocks} block copies", () => { for (int i = 0; i < blocks; i++) { YuvPlanes.CopyBlock(planes.Y, planes.PaddedWidth, (i % planes.MacroblockColumns) * 16, (i / planes.MacroblockColumns % planes.MacroblockRows) * 16, 128f, block); } }, 3);
			Time($"  {blocks} DCTs", () => { for (int i = 0; i < blocks; i++) { Dct8x8.Forward(block, dct); } }, 3);
			var bits = new BitWriter(1 << 20, stuffJpeg: true);
			Time($"  {blocks * 40} bit fields", () => { bits.Reset(); for (int i = 0; i < blocks * 40; i++) { bits.Write((uint)i, 7); } }, 3);
			using (var mpeg = new Mpeg1Writer(Path.Combine(folder, "b.mpg"), w, h, 30, 85))
			{
				Time("mpeg-1 picture", () => mpeg.Add(frame), 5);
			}
			using (var avi = new AviWriter(Path.Combine(folder, "b.avi"), w, h, 30, 100))
			{
				Time("avi frame (uncompressed)", () => avi.Add(frame), 5);
			}
			if (OperatingSystem.IsWindows() && ExportFormats.IsAvailable(ExportFormat.Mp4))
			{
				using var mp4 = new Mp4Writer(Path.Combine(folder, "b.mp4"), w, h, 30, 85);
				Console.WriteLine("  mp4: " + (mp4.Nv12Input ? "NV12" : "RGB32") + " in, " + mp4.Transforms + (mp4.HasDirect3D ? ", Direct3D device shared" : ", system memory") + "; hardware H.264 encoders registered: " + string.Join(", ", Mp4Writer.AvailableEncoders(hardwareOnly: true).DefaultIfEmpty("none")));
				Time("mp4 frame (Media Foundation)", () => mp4.Add(frame), 10);
			}
			// the sequence sink's workers: PNG with one worker and with the default, ten frames each
			foreach (int workers in new[] { 1, ExportFormats.SequenceWorkers(encoderIsParallel: false) })
			{
				string seqFolder = Path.Combine(folder, "scale" + workers);
				var clock = Stopwatch.StartNew();
				using (var seq = new ImageSequenceSink(seqFolder, "f", ".png", w, h, () => (stream, px) => PngEncoder.Write(stream, px.Span, w, h), workers))
				{
					for (int i = 0; i < 10; i++)
					{
						seq.Add(frame);
					}
				}
				Console.WriteLine($"  png sequence sink, {workers} worker(s): {clock.Elapsed.TotalMilliseconds / 10:0.0} ms per frame");
			}
			// the whole job over a real store: a short run at this size (GPU when it works), then each format
			var project = new Project();
			project.Scene.Width = w;
			project.Scene.Height = h;
			// a long enough run for the waves to fill the canvas (the frames' content decides every lossless ratio), 60 frames spread over it
			project.Simulation.Steps = 3000;
			project.Simulation.VisualizeEvery = 50;
			Source source = project.AddSource();
			source.X = w / 2;
			source.Y = h / 2;
			source.Frequency = 120;
			RunSpec spec = RunSpec.Create(project, null, preferGpu: true);
			using var store = FrameStore.Create(Path.Combine(folder, "bench" + FrameCache.Extension), spec.Width, spec.Height, spec.Layers, spec.FrameCount, spec.VisualizeEvery);
			using (var run = new SimulationRun(spec, store, null))
			{
				run.Start();
				run.Completion.Wait();
				Console.WriteLine("  run: " + run.EngineDescription + ", " + store.Count + " frames");
			}
			ColorLut lut = ColorLut.Build(ColorSchemeStore.LoadBuiltIn(null).First(scheme => scheme.Name == Display.DefaultFieldColorScheme));
			// the MP4 writer alone on the run's real frames, rendered up front: input format and rate control compared
			if (OperatingSystem.IsWindows() && ExportFormats.IsAvailable(ExportFormat.Mp4))
			{
				var rendered = new List<int[]>();
				var values = new Half[w * h];
				for (int i = 0; i < store.Count; i++)
				{
					var px = new int[w * h];
					store.CopyLayer(i, FrameLayer.Field, values);
					FrameRenderer.Render(values, w, h, lut, SampleKind.Field, 30f, px);
					rendered.Add(px);
				}
				// the declared frame rate is varied too: if the time per frame tracks it, the encoder paces itself to real time
				foreach ((string label, bool nv12, bool quality, int fps, bool d3d, bool fastest) in new[] { ("NV12, quality mode, Direct3D", true, true, 30, true, false), ("NV12, quality mode, Direct3D, fastest", true, true, 30, true, true), ("NV12, bitrate mode, Direct3D, fastest", true, false, 30, true, true), ("NV12, quality mode, system memory, fastest", true, true, 30, false, true) })
				{
					Mp4Writer.PreferNv12 = nv12;
					Mp4Writer.QualityMode = quality;
					Mp4Writer.UseDirect3D = d3d;
					Mp4Writer.Fastest = fastest;
					var clock = Stopwatch.StartNew();
					using (var writer = new Mp4Writer(Path.Combine(folder, "real.mp4"), w, h, fps, 85))
					{
						foreach (int[] px in rendered)
						{
							writer.Add(px);
						}
					}
					double psnr = 0;
					int probe = Math.Min(30, rendered.Count - 1);
					if (Ffmpeg != null)
					{
						byte[]? rgb = Decode(Path.Combine(folder, "real.mp4"), w, h, out int n);
						psnr = rgb == null || n <= probe ? 0 : Psnr(rgb.AsSpan(probe * w * h * 3, w * h * 3), rendered[probe], w, h);
					}
					Console.WriteLine($"  mp4 writer alone on {rendered.Count} real frames, {label}: {clock.Elapsed.TotalMilliseconds / rendered.Count:0.0} ms per frame, {new FileInfo(Path.Combine(folder, "real.mp4")).Length / 1024} KB, frame {probe} at {psnr:0.0} dB");
				}
				Mp4Writer.PreferNv12 = true;
				Mp4Writer.QualityMode = true;
				Mp4Writer.UseDirect3D = true;
				Mp4Writer.Fastest = false;
				// the same frames through ffmpeg's encoders, as a reference for what the hardware and a CPU encoder do outside Media Foundation
				if (Ffmpeg != null)
				{
					string raw = Path.Combine(folder, "real.bgra");
					using (var rawStream = new FileStream(raw, FileMode.Create))
					{
						foreach (int[] px in rendered)
						{
							rawStream.Write(System.Runtime.InteropServices.MemoryMarshal.AsBytes(px.AsSpan()));
						}
					}
					foreach ((string label, string[] codec) in new[] { ("h264_nvenc", new[] { "-c:v", "h264_nvenc", "-preset", "fast", "-b:v", "17M" }), ("libx264 ultrafast", new[] { "-c:v", "libx264", "-preset", "ultrafast", "-b:v", "17M" }) })
					{
						var info = new ProcessStartInfo(Ffmpeg) { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
						foreach (string a in new[] { "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "bgra", "-s", $"{w}x{h}", "-r", "30", "-i", raw })
						{
							info.ArgumentList.Add(a);
						}
						foreach (string a in codec)
						{
							info.ArgumentList.Add(a);
						}
						info.ArgumentList.Add(Path.Combine(folder, "ref.mp4"));
						var clock = Stopwatch.StartNew();
						using Process p = Process.Start(info)!;
						string err = p.StandardError.ReadToEnd();
						p.WaitForExit();
						Console.WriteLine(p.ExitCode == 0
							? $"  ffmpeg {label} on the same frames: {clock.Elapsed.TotalMilliseconds / rendered.Count:0.0} ms per frame including the raw read ({new FileInfo(Path.Combine(folder, "ref.mp4")).Length / 1024} KB)"
							: $"  ffmpeg {label}: not available ({err.Trim().Split('\n').LastOrDefault()})");
					}
					File.Delete(raw);
				}
			}
			Func<int, int, int, Action<Stream, ReadOnlyMemory<int>>>? preferredJpeg = ExportFormats.PreferredJpeg;
			foreach ((string name, Func<IFrameSink> open) in new (string, Func<IFrameSink>)[]
			{
				("png sequence", () => ExportFormats.Open(ExportFormat.Png, Path.Combine(folder, "j.png"), w, h, 30, 85, 60)),
				("jpeg sequence (in-house)", () => { ExportFormats.PreferredJpeg = null; return ExportFormats.Open(ExportFormat.Jpeg, Path.Combine(folder, "j.jpg"), w, h, 30, 85, 60); }),
				("jpeg sequence (WIC, the library's choice on Windows)", () => ExportFormats.Open(ExportFormat.Jpeg, Path.Combine(folder, "k.jpg"), w, h, 30, 85, 60)),
				("mpeg-1", () => ExportFormats.Open(ExportFormat.Mpeg1, Path.Combine(folder, "j.mpg"), w, h, 30, 85, 60)),
				("avi", () => ExportFormats.Open(ExportFormat.Avi, Path.Combine(folder, "j.avi"), w, h, 30, 85, 60)),
				("mov (Animation)", () => ExportFormats.Open(ExportFormat.Mov, Path.Combine(folder, "j.mov"), w, h, 30, 85, 60)),
				("mp4", () => ExportFormats.Open(ExportFormat.Mp4, Path.Combine(folder, "j.mp4"), w, h, 30, 85, 60)),
				("mp4, writer not throttling", () => { Mp4Writer.Throttle = false; return ExportFormats.Open(ExportFormat.Mp4, Path.Combine(folder, "k.mp4"), w, h, 30, 85, 60); }),
			})
			{
				if ((name == "mp4" && !(OperatingSystem.IsWindows() && ExportFormats.IsAvailable(ExportFormat.Mp4))) || (name.StartsWith("jpeg sequence (WIC", StringComparison.Ordinal) && preferredJpeg == null))
				{
					continue;
				}
				var layer = new ExportLayer(FrameLayer.Field, lut, 30f, null, 0, null);
				var job = new ExportJob(store, 0, store.Count - 1, [new ExportTarget(layer, name, open)], null);
				var clock = Stopwatch.StartNew();
				job.Start();
				job.Completion.Wait();
				string ratio = job.Targets[0].Sink is MovWriter movSink ? $", {(double)store.Count * w * h * 3 / Math.Max(1, movSink.FrameBytes):0.00} : 1 on the run's frames" : "";
				Console.WriteLine($"  job to {name}: {clock.Elapsed.TotalMilliseconds / store.Count:0.0} ms per frame ({job.State}){ratio}");
				ExportFormats.PreferredJpeg = preferredJpeg;
				Mp4Writer.Throttle = true;
			}
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	/// <summary>The app's JPEG path: the WIC codec called natively at 4:4:4.</summary>
	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	private static byte[] WicEncode(int[] frame, int w, int h, int quality) => new WicJpegEncoder(w, h, quality).Encode(frame);

	/// <summary>The three components' sampling factor bytes from the SOF0 segment, or null when there is none.</summary>
	private static byte[]? SamplingFactors(byte[] jpg)
	{
		int i = 2;
		while (i + 4 <= jpg.Length && jpg[i] == 0xFF)
		{
			int marker = jpg[i + 1];
			int length = (jpg[i + 2] << 8) | jpg[i + 3];
			if (marker == 0xC0 && jpg[i + 9] == 3)
			{
				return [jpg[i + 11], jpg[i + 14], jpg[i + 17]];
			}
			if (marker == 0xDA)
			{
				break;
			}
			i += 2 + length;
		}
		return null;
	}

	private static void Time(string what, Action action, int repeats)
	{
		// warm up past the JIT's tiering (a method is recompiled after 30 calls; timing the first ones gave twice the cost, 2026-10-15)
		for (int i = 0; i < 40; i++)
		{
			action();
		}
		Thread.Sleep(150); // the recompilation happens in the background
		var clock = Stopwatch.StartNew();
		for (int i = 0; i < repeats; i++)
		{
			action();
		}
		Console.WriteLine($"  {what}: {clock.Elapsed.TotalMilliseconds / repeats:0.0} ms");
	}

	// ---------------------------------------------------------------- helpers

	/// <summary>A test picture: a diagonal gradient with a bright moving square and a few saturated pixels, different per frame.</summary>
	private static int[] TestFrame(int w, int h, int index)
	{
		var pixels = new int[w * h];
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				// smooth ramps (no wrap-around: a 4:2:0 format blurs a hard colour edge and that would dominate the PSNR)
				int r = Math.Min(255, x * 255 / Math.Max(1, w - 1) + index * 8);
				int g = y * 255 / Math.Max(1, h - 1);
				int b = Math.Min(255, (x + y) * 255 / (w + h) + index * 12);
				pixels[y * w + x] = unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
			}
		}
		int left = 5 + index * 6;
		for (int y = 8; y < 24; y++)
		{
			for (int x = left; x < left + 16 && x < w; x++)
			{
				pixels[y * w + x] = unchecked((int)0xFFFFFFFF);
			}
		}
		pixels[0] = unchecked((int)0xFFFF0000);
		pixels[w - 1] = unchecked((int)0xFF00FF00);
		pixels[(h - 1) * w] = unchecked((int)0xFF0000FF);
		return pixels;
	}


	private static int[] PadEven(int[] pixels, int width, int height)
	{
		int encodedWidth = (width + 1) & ~1;
		int encodedHeight = (height + 1) & ~1;
		if (encodedWidth == width && encodedHeight == height)
		{
			return pixels;
		}
		var padded = new int[encodedWidth * encodedHeight];
		for (int y = 0; y < encodedHeight; y++)
		{
			ReadOnlySpan<int> row = pixels.AsSpan(Math.Min(y, height - 1) * width, width);
			Span<int> target = padded.AsSpan(y * encodedWidth, encodedWidth);
			row.CopyTo(target);
			if (encodedWidth > width)
			{
				target[width] = row[^1];
			}
		}
		return padded;
	}

	private static string? FindFfmpeg()
	{
		foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
		{
			string candidate = Path.Combine(dir.Trim(), OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
			if (dir.Length > 0 && File.Exists(candidate))
			{
				return candidate;
			}
		}
		return null;
	}

	/// <summary>Decodes a file with ffmpeg to packed RGB24 frames of w × h and counts them; null when ffmpeg failed.</summary>
	private static byte[]? Decode(string path, int w, int h, out int frames)
	{
		frames = 0;
		var info = new ProcessStartInfo(Ffmpeg!)
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		foreach (string arg in new[] { "-v", "error", "-i", path, "-f", "rawvideo", "-pix_fmt", "rgb24", "-" })
		{
			info.ArgumentList.Add(arg);
		}
		using Process process = Process.Start(info)!;
		using var output = new MemoryStream();
		Task<string> errors = process.StandardError.ReadToEndAsync();
		process.StandardOutput.BaseStream.CopyTo(output);
		process.WaitForExit();
		string error = errors.Result;
		if (process.ExitCode != 0 || output.Length == 0)
		{
			Console.WriteLine("  ffmpeg: exit " + process.ExitCode + " " + error.Trim());
			return null;
		}
		if (error.Trim().Length > 0)
		{
			Console.WriteLine("  ffmpeg: " + error.Trim());
		}
		byte[] bytes = output.ToArray();
		frames = bytes.Length / (w * h * 3);
		return bytes;
	}

	/// <summary>Decodes a one-channel file with ffmpeg to 16-bit grey samples of w × h; null when ffmpeg failed or the size is off.</summary>
	private static ushort[]? DecodeGray16(string path, int w, int h)
	{
		var info = new ProcessStartInfo(Ffmpeg!)
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		foreach (string arg in new[] { "-v", "error", "-i", path, "-f", "rawvideo", "-pix_fmt", "gray16le", "-" })
		{
			info.ArgumentList.Add(arg);
		}
		using Process process = Process.Start(info)!;
		using var output = new MemoryStream();
		Task<string> errors = process.StandardError.ReadToEndAsync();
		process.StandardOutput.BaseStream.CopyTo(output);
		process.WaitForExit();
		string error = errors.Result;
		if (process.ExitCode != 0 || output.Length != (long)w * h * 2)
		{
			Console.WriteLine("  ffmpeg: exit " + process.ExitCode + ", " + output.Length + " bytes " + error.Trim());
			return null;
		}
		byte[] bytes = output.ToArray();
		var samples = new ushort[w * h];
		for (int i = 0; i < samples.Length; i++)
		{
			samples[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i * 2));
		}
		return samples;
	}

	/// <summary>
	/// Reads the OpenEXR files <see cref="ExrWriter"/> writes: magic, version 2 without flags, the attributes (channel
	/// "Y" as HALF, no compression, the windows the frame), the offset table and one chunk per scanline. Null with a
	/// note when anything is off.
	/// </summary>
	private static Half[]? ReadExr(byte[] file, int w, int h, out string note)
	{
		note = "";
		if (file.Length < 8 || BinaryPrimitives.ReadInt32LittleEndian(file) != 20000630 || BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(4)) != 2)
		{
			note = "magic or version";
			return null;
		}
		int at = 8;
		bool channelOk = false, compressionOk = false, windowOk = false;
		while (true)
		{
			int end = Array.IndexOf(file, (byte)0, at);
			if (end == at)
			{
				at++;
				break; // the header's terminating null
			}
			string name = Encoding.ASCII.GetString(file, at, end - at);
			at = end + 1;
			end = Array.IndexOf(file, (byte)0, at);
			string type = Encoding.ASCII.GetString(file, at, end - at);
			at = end + 1;
			int size = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at));
			at += 4;
			ReadOnlySpan<byte> value = file.AsSpan(at, size);
			at += size;
			switch (name)
			{
				case "channels":
					channelOk = type == "chlist" && value[0] == (byte)'Y' && value[1] == 0 && BinaryPrimitives.ReadInt32LittleEndian(value[2..]) == 1 && value[^1] == 0;
					break;
				case "compression":
					compressionOk = type == "compression" && value[0] == 0;
					break;
				case "dataWindow":
					windowOk = type == "box2i" && BinaryPrimitives.ReadInt32LittleEndian(value) == 0 && BinaryPrimitives.ReadInt32LittleEndian(value[4..]) == 0
						&& BinaryPrimitives.ReadInt32LittleEndian(value[8..]) == w - 1 && BinaryPrimitives.ReadInt32LittleEndian(value[12..]) == h - 1;
					break;
			}
		}
		if (!channelOk || !compressionOk || !windowOk)
		{
			note = "header: channel " + channelOk + ", compression " + compressionOk + ", window " + windowOk;
			return null;
		}
		var offsets = new long[h];
		for (int y = 0; y < h; y++)
		{
			offsets[y] = (long)BinaryPrimitives.ReadUInt64LittleEndian(file.AsSpan(at));
			at += 8;
		}
		var values = new Half[w * h];
		for (int y = 0; y < h; y++)
		{
			int chunk = (int)offsets[y];
			if (chunk + 8 + w * 2 > file.Length || BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(chunk)) != y || BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(chunk + 4)) != w * 2)
			{
				note = "chunk " + y;
				return null;
			}
			for (int x = 0; x < w; x++)
			{
				values[y * w + x] = BinaryPrimitives.ReadHalfLittleEndian(file.AsSpan(chunk + 8 + x * 2));
			}
		}
		return values;
	}

	/// <summary>For a failure message: the first pixel where RGB24 bytes differ from the frame, with both values.</summary>
	private static string Mismatch(byte[]? rgb, int[] frame, int w, int h)
	{
		if (rgb == null)
		{
			return " (no output)";
		}
		if (rgb.Length != w * h * 3)
		{
			return " (" + rgb.Length + " bytes, expected " + w * h * 3 + ")";
		}
		for (int i = 0; i < w * h; i++)
		{
			int argb = frame[i];
			if (rgb[i * 3] != ((argb >> 16) & 0xFF) || rgb[i * 3 + 1] != ((argb >> 8) & 0xFF) || rgb[i * 3 + 2] != (argb & 0xFF))
			{
				return " (pixel " + i + " at " + i % w + "," + i / w + ": got " + rgb[i * 3] + "," + rgb[i * 3 + 1] + "," + rgb[i * 3 + 2] + ", expected " + ((argb >> 16) & 0xFF) + "," + ((argb >> 8) & 0xFF) + "," + (argb & 0xFF) + ")";
			}
		}
		return "";
	}

	/// <summary>PSNR in dB of RGB24 bytes against a BGRA32 frame; 99 for identical.</summary>
	private static double Psnr(ReadOnlySpan<byte> rgb, int[] frame, int w, int h)
	{
		double sum = 0;
		int n = w * h;
		for (int i = 0; i < n; i++)
		{
			int argb = frame[i];
			int dr = rgb[i * 3] - ((argb >> 16) & 0xFF);
			int dg = rgb[i * 3 + 1] - ((argb >> 8) & 0xFF);
			int db = rgb[i * 3 + 2] - (argb & 0xFF);
			sum += dr * dr + dg * dg + db * db;
		}
		double mse = sum / (3.0 * n);
		return mse == 0 ? 99 : 10 * Math.Log10(255.0 * 255.0 / mse);
	}

	/// <summary>Decodes an 8-bit RGB PNG with any of the five filters; null when it is not one.</summary>
	/// <summary>The filter byte of every row of a PNG (the first byte of each inflated row), or null when the file does not inflate.</summary>
	private static byte[]? FilterBytes(byte[] png, int w, int h)
	{
		using var idat = new MemoryStream();
		int p = 8;
		while (p + 8 <= png.Length)
		{
			int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(p));
			string type = Encoding.ASCII.GetString(png, p + 4, 4);
			if (type == "IDAT")
			{
				idat.Write(png, p + 8, length);
			}
			p += 12 + length;
		}
		idat.Position = 0;
		var rows = new byte[(w * 3 + 1) * h];
		using (var zlib = new ZLibStream(idat, CompressionMode.Decompress))
		{
			if (zlib.ReadAtLeast(rows, rows.Length, throwOnEndOfStream: false) != rows.Length)
			{
				return null;
			}
		}
		var filters = new byte[h];
		for (int y = 0; y < h; y++)
		{
			filters[y] = rows[y * (w * 3 + 1)];
		}
		return filters;
	}

	private static int[]? DecodePng(byte[] png, int w, int h)
	{
		int p = 8;
		using var idat = new MemoryStream();
		while (p + 8 <= png.Length)
		{
			int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(p));
			string type = Encoding.ASCII.GetString(png, p + 4, 4);
			if (type == "IHDR" && (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(p + 8)) != w || BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(p + 12)) != h || png[p + 16] != 8 || png[p + 17] != 2))
			{
				return null;
			}
			if (type == "IDAT")
			{
				idat.Write(png, p + 8, length);
			}
			if (Crc32.Of(png.AsSpan(p + 4, 4 + length)) != BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(p + 8 + length)))
			{
				return null;
			}
			p += 12 + length;
		}
		idat.Position = 0;
		using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
		int rowBytes = w * 3;
		var raw = new byte[(rowBytes + 1) * h];
		zlib.ReadExactly(raw);
		var previous = new byte[rowBytes];
		var current = new byte[rowBytes];
		var pixels = new int[w * h];
		for (int y = 0; y < h; y++)
		{
			int filter = raw[y * (rowBytes + 1)];
			for (int i = 0; i < rowBytes; i++)
			{
				int x = raw[y * (rowBytes + 1) + 1 + i];
				int a = i >= 3 ? current[i - 3] : 0;
				int b = previous[i];
				int c = i >= 3 ? previous[i - 3] : 0;
				int predictor = filter switch
				{
					1 => a,
					2 => b,
					3 => (a + b) / 2,
					4 => Paeth(a, b, c),
					_ => 0,
				};
				current[i] = (byte)(x + predictor);
			}
			for (int x = 0; x < w; x++)
			{
				pixels[y * w + x] = unchecked((int)0xFF000000) | (current[x * 3] << 16) | (current[x * 3 + 1] << 8) | current[x * 3 + 2];
			}
			(previous, current) = (current, previous);
		}
		return pixels;
	}

	private static int Paeth(int a, int b, int c)
	{
		int p = a + b - c;
		int pa = Math.Abs(p - a);
		int pb = Math.Abs(p - b);
		int pc = Math.Abs(p - c);
		return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
	}

	private static int CountStartCodes(byte[] data, byte code)
	{
		int count = 0;
		for (int i = 0; i + 3 < data.Length; i++)
		{
			if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 1 && data[i + 3] == code)
			{
				count++;
			}
		}
		return count;
	}

	private static string Fourcc(byte[] data, long offset) => Encoding.ASCII.GetString(data, (int)offset, 4);

	private sealed class AviWalk
	{
		public int Segments;
		public int Frames;
		public int FramesInFirst;
		public int Idx1Entries;
		public int StandardIndexes;
		public int SuperEntries;
		public int TotalInDmlh;
		public long FirstRiffSize;
		public long FirstSegmentBytes;
		public bool EndsAtFileEnd;
	}

	/// <summary>Walks the RIFF segments of an AVI, counting frames, indexes and the OpenDML entries.</summary>
	private static AviWalk WalkAvi(byte[] data)
	{
		var walk = new AviWalk();
		long p = 0;
		while (p + 12 <= data.Length && Fourcc(data, p) == "RIFF")
		{
			long riffSize = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)p + 4));
			long end = p + 8 + riffSize;
			bool first = walk.Segments == 0;
			if (first)
			{
				walk.FirstRiffSize = riffSize;
				walk.FirstSegmentBytes = end;
			}
			walk.Segments++;
			long q = p + 12;
			while (q + 8 <= end)
			{
				string id = Fourcc(data, q);
				int size = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan((int)q + 4));
				if (id == "LIST")
				{
					string type = Fourcc(data, q + 8);
					if (type == "movi")
					{
						long r = q + 12;
						long moviEnd = q + 8 + size;
						while (r + 8 <= moviEnd)
						{
							string chunk = Fourcc(data, r);
							int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan((int)r + 4));
							if (chunk == "00db")
							{
								walk.Frames++;
								if (first)
								{
									walk.FramesInFirst++;
								}
							}
							else if (chunk == "ix00")
							{
								walk.StandardIndexes++;
							}
							r += 8 + chunkSize + (chunkSize & 1);
						}
					}
					else if (type == "hdrl")
					{
						// find indx inside strl, and dmlh is in the odml list that follows hdrl
						int at = IndexOf(data, "indx"u8, (int)q, (int)(q + 8 + size));
						if (at >= 0)
						{
							walk.SuperEntries = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(at + 12));
						}
					}
					else if (type == "odml")
					{
						walk.TotalInDmlh = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan((int)q + 20));
					}
				}
				else if (id == "idx1")
				{
					walk.Idx1Entries = size / 16;
				}
				q += 8 + size + (size & 1);
			}
			p = end;
		}
		walk.EndsAtFileEnd = p == data.Length;
		return walk;
	}

	private static int IndexOf(byte[] data, ReadOnlySpan<byte> needle, int from, int to)
	{
		for (int i = from; i + needle.Length <= Math.Min(to, data.Length); i++)
		{
			if (data.AsSpan(i, needle.Length).SequenceEqual(needle))
			{
				return i;
			}
		}
		return -1;
	}
}
