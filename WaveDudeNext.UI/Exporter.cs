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
using System.Globalization;
using NLog;
using WaveDudeNext.Engine;
using WaveDudeNext.Export;
using WaveDudeNext.Model;
using WaveDudeNext.Probes;
using WaveDudeNext.Rendering;
using WaveDudeNext.Runs;
using WaveDudeNext.UI.Rendering;

namespace WaveDudeNext.UI;

/// <summary>An export failed, before or after it started: what the application tells the user, by the key of its text, with the error when there is one.</summary>
/// <param name="Key">The string table's key: "ExportError" or "RunError".</param>
/// <param name="Error">The error, or null when the message alone says it.</param>
public sealed record ExportFailure(string Key, Exception? Error);

/// <summary>
/// Carries out the exports (2026-10-14; a model of this library since 2026-11-07, the main window's command handlers
/// before): the Save dialog picks the format by its filter and the name, pre-set to the project's name with
/// <c>_field</c> / <c>_intensity</c> in the folder <see cref="ExportPaths"/> proposes; "both" writes two outputs from one
/// name; a still writes the frame the preview shows as one image file (<c>_field_0655</c>, the frame number padded as
/// a sequence names its files). Every export renders from the run's store with the display settings as they are, the
/// outline when opted in and what the application draws over the frames (<see cref="IFrameDecorators"/>: the probe
/// graph boxes and markers, the source markers); a still shows exactly what a video export would show at that frame
/// (the user's rule), never the scratch frame. There is always a run to export: when there is none, it is stale, it is a
/// reduced preview or it lacks the intensity plane, a run is started first (like Play does) and the export waits for it
/// (<see cref="IsWaiting"/>). The <see cref="ExportJob"/> works on its own thread and reports to the busy indicator;
/// Esc cancels it (<see cref="Cancel"/>). Runs are not started or replaced while an export reads the store. A probe's
/// recording goes out as a CSV file the same way (2026-10-18): the dialog, the full run, then the run's trace of the
/// probe written by <see cref="ProbeCsv"/> on the UI thread (a few megabytes at most). Export last (2026-10-28) is the
/// session's last run export again, no dialog and no question. A failure is raised as <see cref="Failed"/> for the
/// application to show; the harness hook <c>WAVEDUDE_EXPORT_PATH</c> stands in for the dialog. UI thread only.
/// </summary>
public sealed class Exporter : ObservableObject
{
	private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

	private readonly ProjectDocument _document;
	private readonly RunHost _runs;
	private readonly PreviewState _preview;
	private readonly BusyState _busy;
	private readonly SceneComposer _scene;
	private readonly ExportPaths _paths;
	private readonly IExportDialogs _dialogs;
	private readonly IFrameDecorators _decorators;
	private readonly SynchronizationContext _uiThread;
	private ExportJob? _job;
	private bool _isWaiting;

	/// <param name="document">The open project: what is exported, and the name its files take.</param>
	/// <param name="runs">The run exported, started first when the current one will not do.</param>
	/// <param name="preview">The position a still is taken at.</param>
	/// <param name="busy">Told the job and the wait for its run.</param>
	/// <param name="scene">The scene pixels the outline is computed from.</param>
	/// <param name="paths">Where the dialogs start and where the exports went.</param>
	/// <param name="dialogs">The Save dialog and the folder question.</param>
	/// <param name="decorators">What the application draws over the frames.</param>
	/// <param name="uiThread">The UI thread for the job's progress and end.</param>
	public Exporter(ProjectDocument document, RunHost runs, PreviewState preview, BusyState busy, SceneComposer scene, ExportPaths paths,
		IExportDialogs dialogs, IFrameDecorators decorators, SynchronizationContext uiThread)
	{
		_document = document;
		_runs = runs;
		_preview = preview;
		_busy = busy;
		_scene = scene;
		_paths = paths;
		_dialogs = dialogs;
		_decorators = decorators;
		_uiThread = uiThread;
	}

	/// <summary>An export failed: the application shows the text of the key, with the error's own words when there is one.</summary>
	public event EventHandler<ExportFailure>? Failed;

	/// <summary>The current job ended, completed, cancelled or failed: a run held back meanwhile may be due.</summary>
	public event EventHandler? Finished;

	/// <summary>The current export job (running or finished), or null.</summary>
	public ExportJob? Job
	{
		get => _job;
		private set
		{
			if (SetProperty(ref _job, value))
			{
				_busy.Export = value;
				OnPropertyChanged(nameof(IsWriting));
				OnPropertyChanged(nameof(IsExporting));
			}
		}
	}

	/// <summary>Whether an export is writing its frames.</summary>
	public bool IsWriting => _job?.IsActive == true;

	/// <summary>Whether an export is waiting for its run to complete before it starts writing.</summary>
	public bool IsWaiting
	{
		get => _isWaiting;
		private set
		{
			if (SetProperty(ref _isWaiting, value))
			{
				_busy.IsWaitingForRun = value;
				OnPropertyChanged(nameof(IsExporting));
			}
		}
	}

	/// <summary>Whether an export is running or waiting for its run.</summary>
	public bool IsExporting => IsWriting || _isWaiting;

	/// <summary>Whether an export can begin: none is in progress, and the project has steps to run.</summary>
	public bool CanExport => !IsExporting && Project.Simulation.Steps > 0;

	/// <summary>Whether Export last has something to repeat: a run export since the project was opened.</summary>
	public bool CanExportLast => _paths.LastRun != null && CanExport;

	/// <summary>Whether <paramref name="parameter"/> is a probe whose data can be exported: an active one, since a disabled one is in no run.</summary>
	public bool CanExportProbe(object? parameter) => parameter is Probe { Active: true } && CanExport;

	private Project Project => _document.Project;

	/// <summary>The Save dialog, the run, the targets, the job.</summary>
	/// <param name="field">Export the field.</param>
	/// <param name="intensity">Export the intensity.</param>
	/// <param name="still">One frame, the one the preview shows, as a single image file.</param>
	public async Task ExportAsync(bool field, bool intensity, bool still = false)
	{
		if (IsExporting)
		{
			return;
		}
		int stillFrame = Math.Max(0, _preview.Position);
		string suffix = field && intensity ? "" : field ? "_field" : "_intensity";
		if (still)
		{
			// the frame number, padded like a sequence's files, so a still of frame 655 is named as the sequence names it
			// (2026-10-19, the user's rule; the step number stood here until then)
			suffix += "_" + stillFrame.ToString("D" + ImageSequenceSink.MinDigits, CultureInfo.InvariantCulture);
		}
		string chosen;
		// the format chosen last is one of the full list; the still's filter lists the sequence formats alone, so a
		// remembered video means PNG there
		ExportFormat[] formats = still ? ExportFormats.Stills : ExportFormats.All;
		int formatIndex = Math.Max(0, Array.IndexOf(formats, _paths.Format));
		string? scripted = Environment.GetEnvironmentVariable("WAVEDUDE_EXPORT_PATH");
		if (!string.IsNullOrEmpty(scripted))
		{
			// a UI harness cannot drive the common dialog by posted keys: it hands the path in instead, "{name}" standing for the suggested name
			chosen = scripted.Replace("{name}", _document.Name + suffix, StringComparison.Ordinal);
			Logger.Debug("Export path taken from WAVEDUDE_EXPORT_PATH: {Path}", chosen);
		}
		else
		{
			// a still keeps the frame's name and takes the last still's folder; a run's export takes the last one's folder and name
			(string folder, string name) = still ? _paths.StartOfStill(_document.Name + suffix) : _paths.StartOfRun(_document.Name + suffix, suffix);
			ExportSubject subject = still ? ExportSubject.Still : field && intensity ? ExportSubject.Both : field ? ExportSubject.Field : ExportSubject.Intensity;
			if (await _dialogs.AskWhereToSaveAsync(subject, folder, name, formats, formatIndex) is not { } pick)
			{
				Logger.Debug("Export cancelled in the Save dialog");
				return;
			}
			chosen = pick.Path;
			formatIndex = Math.Clamp(pick.FormatIndex, 0, formats.Length - 1);
			if (!still)
			{
				_paths.Format = formats[formatIndex];
			}
			_paths.ChosenInDialog(chosen);
		}
		// the filter the user chose is the format, whatever extension was typed (the two MOV formats share theirs); a
		// scripted path has no filter and says its format by its extension, a .mov meaning the Animation codec
		ExportFormat format = formats[formatIndex];
		if (!string.IsNullOrEmpty(scripted))
		{
			format = ExportFormats.FromPath(chosen) ?? format;
		}
		if (still && !ExportFormats.IsSequence(format))
		{
			format = ExportFormat.Png;
		}
		if (!ExportFormats.Accepts(format, chosen))
		{
			chosen = Path.ChangeExtension(chosen, ExportFormats.Extension(format));
		}
		// remembered for the next dialog of the kind, in the settings too, and a run's for Export last
		if (still)
		{
			_paths.StillExported(chosen);
		}
		else
		{
			_paths.RunExported(field, intensity, format, chosen);
		}
		await StartAsync(field, intensity, still, stillFrame, format, chosen, ask: true);
	}

	/// <summary>Export last (Ctrl+E, 2026-10-28, the user's request): the session's last run export again, the same layers, format and path, no dialog and no question about what is there.</summary>
	public async Task ExportLastAsync()
	{
		if (IsExporting || _paths.LastRun is not { } last)
		{
			return;
		}
		Logger.Debug("Export last: the last export made again");
		await StartAsync(last.Field, last.Intensity, still: false, stillFrame: 0, last.Format, last.Path, ask: false);
	}

	/// <summary>The Save dialog, the full run (started first when there is none or it is stale, like the pictures), the probe's trace as CSV.</summary>
	public async Task ExportProbeAsync(Probe probe)
	{
		if (IsExporting)
		{
			return;
		}
		string suggested = ProbeCsv.FileName(_document.Name, probe);
		string chosen;
		string? scripted = Environment.GetEnvironmentVariable("WAVEDUDE_EXPORT_PATH");
		if (!string.IsNullOrEmpty(scripted))
		{
			// the harness hook of the picture exports: "{name}" stands for the suggested name
			chosen = scripted.Replace("{name}", Path.GetFileNameWithoutExtension(suggested), StringComparison.Ordinal);
			Logger.Debug("Export path taken from WAVEDUDE_EXPORT_PATH: {Path}", chosen);
		}
		else
		{
			(string folder, string name) = _paths.StartOfProbe(suggested);
			if (await _dialogs.AskWhereToSaveAsync(ExportSubject.ProbeData, folder, name, [], 0) is not { } pick)
			{
				Logger.Debug("Probe export cancelled in the Save dialog");
				return;
			}
			chosen = pick.Path;
			_paths.ChosenInDialog(chosen);
		}
		_paths.ProbeExported(chosen);
		Logger.Debug("Export: probe '{Probe}' ({Kind}, {Layer}, {Measurement}) as CSV to {Path}", probe.Name, probe.Kind, probe.Layer, probe.Measurement, chosen);
		SimulationRun? run = await EnsureRunAsync(intensity: probe.Layer == ProbeLayer.Intensity, needsHalves: false);
		if (run == null)
		{
			return;
		}
		ProbeTrace? trace = null;
		for (int i = 0; i < run.Spec.Probes.Length && i < run.Traces.Length; i++)
		{
			if (ReferenceEquals(run.Spec.Probes[i].Origin, probe))
			{
				trace = run.Traces[i];
			}
		}
		if (trace == null)
		{
			Logger.Error("Probe '{Probe}' is not in the run (removed or disabled while it was waited for)", probe.Name);
			Failed?.Invoke(this, new ExportFailure("ExportError", null));
			return;
		}
		try
		{
			int rows = ProbeCsv.Write(chosen, probe, trace, run.Spec.VisualizeEvery, run.Spec.FrameStride);
			Logger.Info("Probe data written: {Rows} rows of '{Probe}' to {Path}", rows, probe.Name, chosen);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			Logger.Error(ex, "The probe data could not be written: {Message}", ex.Message);
			Failed?.Invoke(this, new ExportFailure("ExportError", ex));
		}
	}

	/// <summary>Cancels the job in progress, if any; its outputs hold what was written.</summary>
	public void Cancel() => _job?.Cancel();

	/// <summary>Cancels a running export and waits briefly for its files to close; for closing the application.</summary>
	public void Shutdown()
	{
		ExportJob? job = _job;
		if (job?.IsActive != true)
		{
			return;
		}
		job.Cancel();
		if (!job.Completion.Wait(TimeSpan.FromSeconds(10)))
		{
			Logger.Warn("The export did not stop in time; its last file may be incomplete");
		}
	}

	/// <summary>
	/// The run (started first when there is none or it is stale), the targets, the job: the pictures <paramref name="field"/>
	/// and <paramref name="intensity"/> as <paramref name="format"/> at <paramref name="chosen"/>, whose extension is the
	/// format's; a <paramref name="still"/> is the one frame <paramref name="stillFrame"/> as a single image file.
	/// </summary>
	/// <param name="field">Export the field.</param>
	/// <param name="intensity">Export the intensity.</param>
	/// <param name="still">One frame as a single image file.</param>
	/// <param name="stillFrame">The frame of a still.</param>
	/// <param name="format">The format, chosen in the dialog or by the scripted path's extension.</param>
	/// <param name="chosen">The file, its extension the format's; for both layers the name their two files are made from.</param>
	/// <param name="ask">Whether a sequence folder that exists with files in it is replaced only on request; Export last replaces it without asking.</param>
	private async Task StartAsync(bool field, bool intensity, bool still, int stillFrame, ExportFormat format, string chosen, bool ask)
	{
		Logger.Debug("Export: {What} as {Format} to {Path}", still ? "the shown frame " + stillFrame : field && intensity ? "field and intensity" : field ? "field" : "intensity", format, chosen);
		if (!ExportFormats.IsAvailable(format))
		{
			string dependency = OperatingSystem.IsWindows() ? "Media Foundation with an H.264 encoder" : "FFmpeg with the libx264 encoder on PATH";
			var error = new NotSupportedException("MP4 export needs " + dependency + ".");
			Logger.Error(error, "MP4 export is unavailable: {Dependency}", dependency);
			Failed?.Invoke(this, new ExportFailure("ExportError", error));
			return;
		}
		// the output paths: one per layer; "both" takes the chosen name without a layer suffix and appends its own
		var outputs = new List<(FrameLayer Layer, string Path)>();
		if (field && intensity)
		{
			string folder = Path.GetDirectoryName(chosen) ?? "";
			string name = Path.GetFileNameWithoutExtension(chosen);
			foreach (string known in ExportPaths.LayerSuffixes)
			{
				if (name.EndsWith(known, StringComparison.OrdinalIgnoreCase))
				{
					name = name[..^known.Length];
				}
			}
			string extension = ExportFormats.Extension(format);
			outputs.Add((FrameLayer.Field, Path.Combine(folder, name + "_field" + extension)));
			outputs.Add((FrameLayer.Intensity, Path.Combine(folder, name + "_intensity" + extension)));
		}
		else
		{
			outputs.Add((field ? FrameLayer.Field : FrameLayer.Intensity, chosen));
		}
		// a sequence goes to a folder of its own; one that exists with files in it is replaced only on request (Export last asks nothing)
		if (!still && ExportFormats.IsSequence(format))
		{
			foreach ((_, string path) in outputs)
			{
				string folder = ExportFormats.SequenceFolder(path);
				if (!Directory.Exists(folder) || !Directory.EnumerateFileSystemEntries(folder).Any())
				{
					continue;
				}
				if (!ask)
				{
					Logger.Debug("Export last: {Folder} replaced without asking", folder);
				}
				else if (!await _dialogs.AskToReplaceFolderAsync(folder))
				{
					Logger.Debug("Export cancelled: {Folder} kept", folder);
					return;
				}
			}
		}
		SimulationRun? run = await EnsureRunAsync(intensity, needsHalves: true, intensityRoot: intensity ? ExportFormats.IntensityRootFor(format) : null);
		if (run == null)
		{
			return;
		}
		// the range in frames, from the model's steps like the preview's in/out points; a still is the shown frame
		Display display = Project.Display;
		int every = run.Spec.VisualizeEvery;
		int lastFrame = run.Spec.FrameCount - 1;
		int first;
		int last;
		if (still)
		{
			first = last = Math.Clamp(stillFrame, 0, lastFrame);
		}
		else
		{
			first = Math.Clamp(display.ExportFrom / every, 0, lastFrame);
			last = display.ExportTo <= 0 ? lastFrame : Math.Clamp(display.ExportTo / every, first, lastFrame);
		}
		int frameCount = last - first + 1;
		Logger.Debug("Export range: frames {First} to {Last} ({Count}) of the run's {Total}, {Width}×{Height}, store {Store}", first, last, frameCount, run.Spec.FrameCount, run.Spec.Width, run.Spec.Height, run.Store.Path);
		// the targets: the layer's colours and outline, what the application draws over the frames, the sink
		var targets = new List<ExportTarget>();
		try
		{
			byte[]? edges = null;
			if (display.Field.OverlayInExport || display.Intensity.OverlayInExport)
			{
				ScenePixels? scene = _scene.Load();
				if (scene != null && scene.Width == run.Spec.Width && scene.Height == run.Spec.Height)
				{
					edges = GlowingEdges.Compute(scene.Bgra, scene.Width, scene.Height, scene.StrideBytes);
				}
				Logger.Debug("Export outline: {State}", edges != null ? "computed from the scene image" : "none (no image scene)");
			}
			IFrameDecorator decorator = _decorators.Create(Project, run);
			foreach ((FrameLayer layer, string path) in outputs)
			{
				DisplayLayer settings = layer == FrameLayer.Field ? display.Field : display.Intensity;
				ColorLut lut = ColorSchemeCatalog.Instance.LutFor(Project, settings.ColorScheme, settings.BlackLevel);
				var exportLayer = new ExportLayer(layer, lut, (float)settings.Brightness, settings.OverlayInExport ? edges : null, settings.OverlayGain, decorator.For(layer));
				// the sink is opened by the job on its thread (a Media Foundation writer must stay on one thread)
				int width = run.Spec.Width;
				int height = run.Spec.Height;
				int fps = display.Video.Fps;
				int quality = display.Video.Quality;
				string output = !still && ExportFormats.IsSequence(format) ? ExportFormats.SequenceFolder(path) : path;
				Func<IFrameSink> open = still
					? () => ExportFormats.OpenStill(format, path, width, height, quality)
					: () => ExportFormats.Open(format, path, width, height, fps, quality, frameCount, first);
				targets.Add(new ExportTarget(exportLayer, output, open));
				if (ExportFormats.IsData(format))
				{
					Logger.Debug("Export target {Layer}: the stored 16-bit values as OpenEXR{Samples} (no colours, outline or boxes), to {Output}",
						layer, layer == FrameLayer.Intensity ? (run.Store.IntensityRoot ? ", the intensity's square root" : ", the intensity itself") : "", output);
				}
				else
				{
					Logger.Debug("Export target {Layer}: scheme '{Scheme}' (black level {Black}), brightness {Brightness}, outline {Outline}, probe boxes {Boxes}, markers {Markers}, sources {Sources}, to {Output}",
						layer, settings.ColorScheme, settings.BlackLevel, settings.Brightness, settings.OverlayInExport && edges != null ? "gain " + settings.OverlayGain : "off", display.ProbeGraphsInExport ? "on" : "off", display.ProbeMarkersInExport ? "on" : "off", display.SourceMarkersInExport ? "on" : "off", output);
				}
			}
			if (!still && format == ExportFormat.Mpeg1)
			{
				double used = Mpeg1Writer.FrameRates[Mpeg1Writer.NearestFrameRateCode(display.Video.Fps) - 1];
				if (Math.Abs(used - display.Video.Fps) > 0.01)
				{
					Logger.Warn("MPEG-1 has no {Wanted} fps: the video is written at {Used:0.###} fps", display.Video.Fps, used);
				}
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or RunSpecException)
		{
			Logger.Error(ex, "The export could not start: {Message}", ex.Message);
			Failed?.Invoke(this, new ExportFailure("ExportError", ex));
			return;
		}
		var job = new ExportJob(run.Store, first, last, targets, _uiThread);
		job.PropertyChanged += OnJobPropertyChanged;
		job.Finished += (_, _) => OnJobFinished(job);
		Job = job;
		Logger.Info("Export started: frames {First} to {Last} ({Count}) of {Width}×{Height} to {Outputs}",
			first, last, frameCount, run.Spec.Width, run.Spec.Height, string.Join(", ", targets.Select(target => target.Output)));
		job.Start();
	}

	/// <summary>
	/// A completed run the export can read: the current one when it is fresh, finished or in progress, and
	/// holds the intensity plane if asked for; otherwise a run is started and awaited. Null when no run could
	/// be had (logged, and <see cref="Failed"/> raised when the run could not start).
	/// </summary>
	/// <param name="intensity">The run must hold the intensity plane.</param>
	/// <param name="needsHalves">The run must hold 16-bit samples (every picture or data export); the probe data does with the 8-bit index.</param>
	/// <param name="intensityRoot">What the run's 16-bit intensity must hold for an OpenEXR export of it: its square root (true), the intensity (false), or either (null).</param>
	private async Task<SimulationRun?> EnsureRunAsync(bool intensity, bool needsHalves, bool? intensityRoot = null)
	{
		IsWaiting = true;
		try
		{
			for (int attempt = 0; attempt < 3; attempt++)
			{
				SimulationRun? run = _runs.Run;
				// a reduced preview never serves a picture or data export: a smaller picture or skipped frames are not the
				// run, and the 8-bit index has only the palette's 255 colours where the 16-bit samples give every level of the
				// gradient (2026-10-23, the user's choice; from 2026-10-18 a picture export had taken an 8-bit run as it was)
				PreviewQuality? quality = run?.Spec.Quality;
				bool reduced = quality != null && (quality.ResolutionDivisor != 1 || quality.FrameDivisor != 1 || (quality.Format != FrameFormat.Half && needsHalves));
				string? reason = run == null ? "no run" : _runs.IsRunStale ? "the run is stale" : _runs.IsProbesStale ? "the probes were edited since the run"
					: reduced ? (quality!.Format != FrameFormat.Half && needsHalves && quality.ResolutionDivisor == 1 && quality.FrameDivisor == 1 ? "the export needs the 16-bit samples" : "the run is a reduced preview (View menu)")
					: intensity && !run.Store.Has(FrameLayer.Intensity) ? "the run has no intensity plane"
					: intensityRoot is bool root && run.Store.IntensityRoot != root ? (root ? "the export needs the intensity as its square root" : "the export needs the intensity as I, which the preview stores as its square root")
					: !run.IsActive && run.State != RunState.Completed ? "the run did not complete" : null;
				if (reason != null)
				{
					Logger.Debug("Export: a run is started first ({Reason})", reason);
					// a full-quality run asks nothing, so its start is decided within the call; false means another start still
					// waits for its memory warning's answer, and the run that one makes would not be this export's
					if (!await _runs.StartAsync(automatic: true, trackIntensity: intensity, fullQuality: true, intensityRoot: intensityRoot ?? true))
					{
						Logger.Warn("Export abandoned: another run's start waits for the memory warning's answer");
						return null;
					}
					run = _runs.Run;
				}
				else if (run!.IsActive)
				{
					Logger.Debug("Export: waiting for the run in progress");
				}
				if (run == null)
				{
					return null;
				}
				await run.Completion;
				await WaitForState(run);
				if (run.State == RunState.Completed && ReferenceEquals(run, _runs.Run))
				{
					return run;
				}
				if (!ReferenceEquals(run, _runs.Run))
				{
					// replaced meanwhile (an edit started a fresh one): look at the new one
					Logger.Debug("Export: the run was replaced while waited for, looking again");
					continue;
				}
				Logger.Warn("Export abandoned: the run ended {State}", run.State);
				return null;
			}
			Logger.Warn("Export abandoned: the project kept changing while its run was computed");
			return null;
		}
		catch (Exception ex) when (ex is RunSpecException or IOException or UnauthorizedAccessException or NotSupportedException)
		{
			Logger.Error(ex, "The export's run could not start: {Message}", ex.Message);
			Failed?.Invoke(this, new ExportFailure("RunError", ex));
			return null;
		}
		finally
		{
			IsWaiting = false;
		}
	}

	/// <summary>The run's final state is posted to the UI thread after its completion task completes; wait for that post.</summary>
	private static async Task WaitForState(SimulationRun run)
	{
		for (int i = 0; i < 50 && !run.IsFinished; i++)
		{
			await Task.Delay(20);
		}
	}

	private void OnJobPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ExportJob.State) && ReferenceEquals(sender, _job))
		{
			OnPropertyChanged(nameof(IsWriting));
			OnPropertyChanged(nameof(IsExporting));
		}
	}

	private void OnJobFinished(ExportJob job)
	{
		if (!ReferenceEquals(job, _job))
		{
			return;
		}
		string outputs = string.Join(", ", job.Targets.Select(target => target.Output));
		switch (job.State)
		{
			case ExportState.Completed:
				Logger.Info("Exported {Frames} frames to {Outputs} in {Seconds:0.0} s", job.FramesDone, outputs, job.Elapsed.TotalSeconds);
				break;
			case ExportState.Cancelled:
				Logger.Warn("Export cancelled after {Frames} of {Total} frames; {Outputs} hold what was written", job.FramesDone, job.FrameCount, outputs);
				break;
			default:
				Logger.Error("Export failed after {Frames} frames: {Message}", job.FramesDone, job.Error?.Message);
				if (job.Error != null)
				{
					Failed?.Invoke(this, new ExportFailure("ExportError", job.Error));
				}
				break;
		}
		Finished?.Invoke(this, EventArgs.Empty);
	}
}
