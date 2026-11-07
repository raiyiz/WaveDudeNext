
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using WaveDudeNext.Engine;

using Avalonia.Platform.Storage;
using WaveDudeNext.Model;
using WaveDudeNext.Runs;
using WaveDudeNext.Parts;
using WaveDudeNext.Avalonia.Rendering;

using System.Reflection;
using System.IO;
using System.Linq;



namespace WaveDudeNext.Avalonia;


public partial class MainWindow : Window
{
    private const int WidthPixels = 256;
    private const int HeightPixels = 256;

    private IWaveEngine _engine = null!;
	private WriteableBitmap _fieldBitmap = null!;
	private WriteableBitmap _intensityBitmap = null!;
    private readonly DispatcherTimer _timer;
    private readonly SceneImageWatch _sceneImageWatch;
    
    private ProjectWatcher? _projectWatcher;
	private ProjectHistory? _projectHistory;
	private bool _hasPendingChanges;
	private bool _needsSimulationRebuild;

    private int _step;
    private int _runSteps = int.MaxValue;
    private int _visualizeEvery = 4;

    public MainWindow()
    {
        InitializeComponent();
        _sceneImageWatch = new SceneImageWatch(message => StatusText.Text = message);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += async (_, _) => { _backgroundTask = UpdateBackground(); await _backgroundTask; };
        InitializeWorkspace();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _editTimer.Stop();
            StopPlayback();
            _frames?.Dispose();
            _sceneImageWatch.Dispose();
            _projectWatcher?.Dispose();
            CancelStepping();
            lock (_engineGate) (_engine as IDisposable)?.Dispose();
            _fieldBitmap.Dispose();
            _intensityBitmap.Dispose();
        };
    }


	private Project? _loadedProject;
    private string? _projectPath;

	private async void OpenProject_Click(
		object? sender,
		global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		var files = await StorageProvider.OpenFilePickerAsync(
		    new FilePickerOpenOptions
		    {
		        Title = "Open WaveDudeNext Project",
		        AllowMultiple = false,
		        FileTypeFilter =
		        [
		            new FilePickerFileType("WaveDudeNext Project")
		            {
		                Patterns = ["*.wdproj"]
		            }
		        ]
		    });

		if (files.Count == 0)
		    return;

		try
		{
		    var path = files[0].TryGetLocalPath();

		    if (path is null)
		        throw new NotSupportedException(
		            "Only local project files are supported.");

            if (!await ConfirmProjectReplacement()) return;
            InstallProject(Project.Load(path), path);
            StatusText.Text = $"Loaded: {Path.GetFileName(path)}";

		    Console.WriteLine(
		        $"Loaded project: {path}");
		}
		catch (Exception ex)
		{
		    StatusText.Text =
		        $"Could not load project: {ex.Message}";

		    Console.WriteLine(ex);
		}
	}
	
	
	
    private async void SaveProject_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveProjectAsync(saveAs: false);
    }

    private async void SaveProjectAs_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveProjectAsync(saveAs: true);
    }

    private async System.Threading.Tasks.Task<bool> SaveProjectAsync(bool saveAs)
    {
        if (_loadedProject is null)
        {
            StatusText.Text = "Open a project before saving.";
            return false;
        }

        string? temporaryPath = null;
        string? previousImagePath = null;
        try
        {
            var path = _projectPath;
            if (saveAs || path is null)
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save WaveDudeNext Project",
                    SuggestedFileName = path is null ? "Project.wdproj" : Path.GetFileName(path),
                    DefaultExtension = "wdproj",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = [new FilePickerFileType("WaveDudeNext Project")
                    {
                        Patterns = ["*.wdproj"]
                    }]
                });
                if (file is null)
                    return false;
                path = file.TryGetLocalPath()
                    ?? throw new NotSupportedException("Only local project files are supported.");
            }

            // Serialize beside the destination before replacing it, so a failed save
            // leaves the previous project intact. Pending editor changes are included.
            temporaryPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!,
                $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            previousImagePath = _loadedProject.Scene.ImagePath;
            string resolvedImagePath = _loadedProject.Scene.ResolveImagePath(PropertiesEditor.ProjectFolder);
            if (resolvedImagePath.Length > 0)
                _loadedProject.Scene.ImagePath = Scene.StoredImagePath(Path.GetDirectoryName(path), resolvedImagePath);
            _loadedProject.Save(temporaryPath);
            File.Move(temporaryPath, path, overwrite: true);
            temporaryPath = null;
            previousImagePath = null;
            _projectHistory?.Record();
            _projectHistory?.MarkSaved();
            _projectPath = path;
            RememberProject(path);
            PropertiesEditor.ProjectFolder = Path.GetDirectoryName(path);
            _sceneImageWatch.Attach(_loadedProject.Scene, PropertiesEditor.ProjectFolder, refresh: false);
            StatusText.Text = $"Saved: {Path.GetFileName(path)}";
            Title = "WaveDude Next — " + Path.GetFileNameWithoutExtension(path);
            return true;
        }
        catch (Exception ex)
        {
            if (previousImagePath is not null) _loadedProject.Scene.ImagePath = previousImagePath;
            StatusText.Text = $"Could not save project: {ex.Message}";
            Console.WriteLine(ex);
            return false;
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { File.Delete(temporaryPath); }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }
    }

	private void ApplyChanges_Click(
		object? sender,
		global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (_loadedProject is null || !_hasPendingChanges)
		    return;

		try
		{
		    // Save the edited project state as an undo step.
		    _projectHistory?.Record();

		    if (_needsSimulationRebuild || (_frames?.Format == FrameFormat.Index &&
                (_frames.Shape.FieldBrightness != (float)_loadedProject.Display.Field.Brightness || _frames.Shape.IntensityBrightness != (float)_loadedProject.Display.Intensity.Brightness)))
		    {
		        // Recreates the optical scene and GPU engine.
		        LoadSimulation(_loadedProject);
		    }

		    _hasPendingChanges = false;
		    _needsSimulationRebuild = false;

		    StatusText.Text = "Changes applied";
		}
		catch (Exception ex)
		{
		    StatusText.Text =
		        $"Could not apply changes: {ex.Message}";

		    Console.WriteLine(ex);
		}
	}
	
	
	private float GetVisibleFieldMaximum()
	{
		float maximum = 0f;

		int width = _engine.Width;
		int height = _engine.Height;
		int stride = _engine.Stride;
		int rim = _engine.Rim;

		var data = _engine.FieldData;

		for (int y = 0; y < height; y++)
		{
		    for (int x = 0; x < width; x++)
		    {
		        int index =
		            (y + rim + 1) * stride +
		            x + rim + 1;

		        maximum = MathF.Max(
		            maximum,
		            MathF.Abs(data[index]));
		    }
		}

		return maximum;
	}

	
	
	
	
	private void AttachProjectTracking(Project project)
	{
		_projectWatcher?.Dispose();

		_projectWatcher = new ProjectWatcher(project);
		_projectHistory = new ProjectHistory(project);

		_hasPendingChanges = false;
		_needsSimulationRebuild = false;

		_projectWatcher.Changed += OnProjectChanged;
        _sceneImageWatch.Attach(project.Scene, PropertiesEditor.ProjectFolder);
	}

	private void OnProjectChanged(
		object? sender,
		ProjectChange change)
	{
        _fieldLut = _intensityLut = null;
        FieldCanvas.InvalidateVisual();
        IntensityCanvas.InvalidateVisual();
        if (AutoApply.IsChecked == true)
        {
            _editTimer.Stop();
            _editTimer.Start();
        }
		_hasPendingChanges = true;

		if (change.AffectsSimulation || change.AffectsProbes)
		    _needsSimulationRebuild = true;


	}

	
	
	
	private void LoadSimulation(Project project, string? projectFolder = null)
    {
        CancelStepping();
        lock (_engineGate) LoadSimulationCore(project, projectFolder);
    }
    private void LoadSimulationCore(Project project, string? projectFolder)
	{
		
        var paintedScene = SceneImageLoader.Compose(project, projectFolder ?? PropertiesEditor.ProjectFolder);

		var spec = RunSpec.Create(
			project,
			paintedScene,
			preferGpu: true,
			trackIntensity: true
		);
		
		SceneData scene = spec.Image is { } image
			? SceneData.FromBgra(
				image.Bgra,
				image.Width,
				image.Height,
				image.StrideBytes,
				spec.ImageSources,
				spec.DampingEdge,
				spec.Edges == EdgeMode.Open)
			: SceneData.Empty(
				spec.Width,
				spec.Height,
				spec.DampingEdge,
				spec.RefractiveIndex,
				spec.Edges == EdgeMode.Open);

		// Preserve source configurations from the project.
		var newEngine = WaveEngineFactory.Create(
		    scene,
		    spec.Engine,
		    spec.PreferGpu,
		    spec.Emitters,
		    out string note
		);

		// Create the new frame buffers before replacing the old ones.
		var shape = new FrameShape(spec.Width, spec.Height, _quality.ResolutionDivisor, _quality.Format, 1, 1);
        var size = new PixelSize(shape.Width, shape.Height);
		var dpi = new Vector(96, 96);

		var newFieldBitmap = new WriteableBitmap(
		    size, dpi, PixelFormats.Bgra8888, AlphaFormat.Opaque);

		var newIntensityBitmap = new WriteableBitmap(
		    size, dpi, PixelFormats.Bgra8888, AlphaFormat.Opaque);

		_timer.Stop();

		// Detach old bitmap references from Avalonia.
		FieldImage.Source = null;
		IntensityImage.Source = null;

		var oldEngine = _engine;
		var oldFieldBitmap = _fieldBitmap;
		var oldIntensityBitmap = _intensityBitmap;

		// Install the new simulation.
		_engine = newEngine;
        _runSteps = spec.Steps;
        _visualizeEvery = spec.VisualizeEvery;
		_fieldBitmap = newFieldBitmap;
		_intensityBitmap = newIntensityBitmap;

		FieldImage.Source = _fieldBitmap;
		IntensityImage.Source = _intensityBitmap;

		_step = 0;

		// Release previous resources.
		(oldEngine as IDisposable)?.Dispose();
		oldFieldBitmap?.Dispose();
		oldIntensityBitmap?.Dispose();


		_step = 0;

		Console.WriteLine(
		    $"Project engine: {_engine.Description}");

		if (!string.IsNullOrEmpty(note))
		    Console.WriteLine(note);

        _previewProject = project;
        foreach (var canvas in new[] { FieldCanvas, IntensityCanvas })
        {
            canvas.Project = project;
            canvas.CanvasWidth = spec.Width;
            canvas.CanvasHeight = spec.Height;
        }
        _overlayEdges = paintedScene is null ? null : WaveDudeNext.Rendering.GlowingEdges.Compute(
            paintedScene.Bgra, paintedScene.Width, paintedScene.Height, paintedScene.StrideBytes);
        ResetTimeline();
		RenderFrame();
	}
    

    private void Start_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopPlayback();
        if (_hasPendingChanges) ApplyPendingChanges();
        if (_step >= _runSteps)
        {
            CancelStepping();
            lock (_engineGate) _engine.Reset();
            _step = 0;
            ResetTimeline();
            RenderFrame();
        }
        _timer.Start();
    }

    private void Pause_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _timer.Stop();
        _stepCancel?.Cancel();
    }

    private void Reset_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _timer.Stop();
        CancelStepping();
        lock (_engineGate) _engine.Reset();
        _step = 0;
        ResetTimeline();
        RenderFrame();
    }
    
    
    
    
    
	private void Undo_Click(
		object? sender,
		global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		MoveInHistory(undo: true);
	}

	private void Redo_Click(
		object? sender,
		global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		MoveInHistory(undo: false);
	}

	private void MoveInHistory(bool undo)
	{
		if (_loadedProject is null ||
		    _projectHistory is null)
		    return;

		// Don't lose edits that haven't been applied yet.
		if (_hasPendingChanges)
		{
            _projectHistory.Record();
            _hasPendingChanges = false;
		}

		try
		{
		    bool changed = undo
		        ? _projectHistory.Undo()
		        : _projectHistory.Redo();

		    if (!changed)
		    {
		        StatusText.Text = undo
		            ? "Nothing to undo"
		            : "Nothing to redo";
		        return;
		    }

		    // Undo/Redo updates the existing Project model.
		    // Recreate the GPU simulation using restored values.
            _sceneImageWatch.Attach(_loadedProject.Scene, PropertiesEditor.ProjectFolder, refresh: false);
		    LoadSimulation(_loadedProject);

		    // History may replace objects inside collections,
		    // so rebuild the tree to avoid stale references.
		    ProjectTreeControl.LoadProject(_loadedProject);

		    SelectObject(null);

		    // ProjectWatcher also observes Undo/Redo,
		    // so clear its pending flags afterward.
		    _hasPendingChanges = false;
		    _needsSimulationRebuild = false;

		    StatusText.Text = undo
		        ? "Undo successful"
		        : "Redo successful";
		}
		catch (Exception ex)
		{
		    StatusText.Text =
		        $"History operation failed: {ex.Message}";

		    Console.WriteLine(ex);
		}
	}

    
    
    
    
    
    
    
    
    
    

    private void UpdateSimulation()
    {
        int steps = _runSteps;
        int interval = StoredEvery;
        int count = Math.Min(interval, Math.Max(0, steps - _step));
        for (int i = 0; i < count; i++)
        {
            _engine.AdvanceOneStep();
            _engine.SampleProbes();
            _step++;
        }
        if (_step >= steps)
            _timer.Stop();

        RenderFrame();
    }
    
    
}
