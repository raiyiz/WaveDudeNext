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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WaveDudeNext.Runs;
using SkiaSharp;
using WaveDudeNext.Avalonia;
using WaveDudeNext.Avalonia.Controls;
using WaveDudeNext.Avalonia.Rendering;
using WaveDudeNext.Avalonia.Views;
using WaveDudeNext.Model;

if (args.Contains("--workspace"))
{
    global::Avalonia.Logging.Logger.Sink = new EditorLog();
    AppBuilder.Configure<WorkspaceCheckApp>().UsePlatformDetect().WithInterFont()
        .StartWithClassicDesktopLifetime(args);
    return;
}

// Exercise the real compiled views and bindings without an X server or desktop windows.
AppBuilder.Configure<App>().UseSkia().UseHarfBuzz().WithInterFont()
    .UseStandardRuntimePlatformSubsystem()
    .UseWindowingSubsystem(() => { }, "checks").SetupWithoutStarting();

void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    Console.WriteLine("PASS " + message);
}

T[] Children<T>(Control control) where T : class => control.GetLogicalDescendants().OfType<T>().ToArray();

var project = new Project();
var projectView = new ProjectView { DataContext = project };
var sceneView = new SceneView { DataContext = project.Scene };
var simulationView = new SimulationView { DataContext = project.Simulation };
foreach (var view in new Control[] { projectView, sceneView, simulationView })
{
    view.ApplyStyling();
    foreach (var element in view.GetLogicalDescendants().OfType<StyledElement>())
        element.ApplyStyling();
}
Dispatcher.UIThread.RunJobs();
Check(Children<FormPanel>(projectView).Length == 1 && Children<FormPanel>(sceneView).Length == 2
    && Children<FormPanel>(simulationView).Length == 2, "WPF form sections are preserved");

Check(Application.Current!.TryGetResource("SectionGeneral", null, out var general) && general as string == "General"
    && Application.Current.TryGetResource("Label.Project.Seed", null, out var seedLabel) && seedLabel as string == "Random seed",
    "WPF section and field strings are available to Avalonia");
var seed = Children<NumericUpDown>(projectView).Single();
Check(seed.Value == project.Seed, "Project seed binding initializes");
seed.Value = -12345;
Check(project.Seed == -12345, "Project seed accepts signed integers");
Children<Button>(projectView).Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Check(project.Seed != -12345, "New seed button calls the model");
var description = Children<TextBox>(projectView).Single();
description.Text = "Linux editor check";
Check(project.Description == description.Text, "Project description edits reach the model");

var simulationNumbers = Children<NumericUpDown>(simulationView);
Check(simulationNumbers.Length == 6, "All WPF simulation numeric settings are present");
simulationNumbers[0].Value = 17;
simulationNumbers[1].Value = 3;
simulationNumbers[2].Value = 0.75m;
simulationNumbers[3].Value = 2.5m;
simulationNumbers[4].Value = 1.2m;
simulationNumbers[5].Value = 0.02m;
Check(project.Simulation.Steps == 17 && project.Simulation.VisualizeEvery == 3
    && project.Simulation.WaveSpeed == 0.75 && project.Simulation.TimeDecay == 2.5
    && project.Simulation.SourceFrequencyScale == 1.2 && project.Simulation.IntensityScale == 0.02,
    "All simulation numeric bindings update the model");
Children<CheckBox>(simulationView).Single().IsChecked = false;
Check(!project.Simulation.FourthOrderLaplacian, "Solver checkbox updates the model");
project.Simulation.Steps = 29;
Check(simulationNumbers[0].Value == 29, "Simulation controls follow model changes");

var sceneNumbers = Children<NumericUpDown>(sceneView);
Check(sceneNumbers.Length == 5, "All WPF scene numeric settings are present");
Check(sceneNumbers[0].IsEnabled, "Canvas width is editable in canvas mode");
var mode = Children<ComboBox>(sceneView)[0];
mode.SelectedItem = SceneMode.Image;
Check(project.Scene.Mode == SceneMode.Image && !sceneNumbers[0].IsEnabled && !sceneNumbers[1].IsEnabled,
    "Image mode disables canvas dimensions");
mode.SelectedItem = SceneMode.Canvas;
Check(sceneNumbers[0].IsEnabled && sceneNumbers[1].IsEnabled, "Canvas mode re-enables dimensions");
Children<ComboBox>(sceneView)[1].SelectedItem = EdgeMode.Reflective;
Check(project.Scene.Edges == EdgeMode.Reflective, "Edge mode binding updates the model");
var sceneChecks = Children<CheckBox>(sceneView);
sceneChecks[0].IsChecked = true;
sceneNumbers[4].Value = 1.5m;
sceneChecks[1].IsChecked = false;
Check(project.Scene.ImageSources && project.Scene.ImageSourceScale == 1.5 && !project.Scene.ObjectSources,
    "Image and object source controls update the model");

// Distinct RGB values verify the physics channels are not swapped, premultiplied or lost.
using var bitmap = new SKBitmap(new SKImageInfo(8, 9, SKColorType.Bgra8888, SKAlphaType.Unpremul));
bitmap.Erase(new SKColor(100, 180, 23, 255));
using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
byte[] bytes = encoded.ToArray();
var pixels = SceneImageLoader.Load(bytes);
Check(pixels.Width == 8 && pixels.Height == 9 && pixels.StrideBytes == 32
    && pixels.Bgra[0] == 23 && pixels.Bgra[1] == 180 && pixels.Bgra[2] == 100 && pixels.Bgra[3] == 255,
    "Scene decoding preserves BGRA solver channels and dimensions");
using (var translucent = new SKBitmap(new SKImageInfo(8, 8, SKColorType.Bgra8888, SKAlphaType.Unpremul)))
{
    translucent.Erase(new SKColor(101, 181, 23, 128));
    using var data = translucent.Encode(SKEncodedImageFormat.Png, 100);
    var decoded = SceneImageLoader.Load(data.ToArray());
    Check(decoded.Bgra[0] == 23 && decoded.Bgra[1] == 181 && decoded.Bgra[2] == 101 && decoded.Bgra[3] == 128,
        "Transparent pixels retain straight-alpha physics channel values");
}
Check(SceneView.ImageInfo.Convert(bytes, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture)
    is string info && info.Contains("8 × 9"), "Embedded image readout shows its dimensions");
project.Scene.Mode = SceneMode.Image;
project.Scene.Image = "scene.png";
project.Scene.ImageData = bytes;
using var history = new WatchAndHistory(project);
Children<Button>(sceneView).Single(button => button.Name == null && button.GetValue(
    global::Avalonia.Automation.AutomationProperties.AutomationIdProperty) == "SceneRemoveImage")
    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Check(project.Scene.Mode == SceneMode.Canvas && project.Scene.ImageData is null
    && project.Scene.Width == 8 && project.Scene.Height == 9, "Remove image preserves canvas size");
history.History.Record();
Check(history.Changes > 0 && history.History.Undo() && project.Scene.ImageData!.SequenceEqual(bytes),
    "Scene edits participate in watcher tracking and undo");
Check(history.History.Redo() && project.Scene.ImageData is null, "Scene edits participate in redo");

// Project serialization includes all three editors' values.
using var saved = new MemoryStream();
project.Save(saved);
using var input = new MemoryStream(saved.ToArray());
var restored = Project.Load(input);
Check(restored.Description == "Linux editor check" && restored.Simulation.Steps == 29
    && restored.Scene.Edges == EdgeMode.Reflective, "Editor values survive project save/load");
// Every optical kind now resolves to its WPF-derived Avalonia editor.
foreach (var type in typeof(OpticalPart).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(OpticalPart)) && !type.IsAbstract))
{
    var part = project.AddPart(type);
    string name = part is LogicGatePart ? "LogicGatePartView" : type.Name + "View";
    var viewType = typeof(LensPartView).Assembly.GetType("WaveDudeNext.Avalonia.Views." + name)!;
    var view = (Control)Activator.CreateInstance(viewType)!;
    view.DataContext = part;
    var position = Children<NumericUpDown>(view).First(box => box.GetValue(global::Avalonia.Automation.AutomationProperties.AutomationIdProperty) == "PartX");
    position.Value = 123;
    Check(part.X == 123, type.Name + " editor exposes the shared position binding");
}
project.Parts.Clear();
var sourceModel = project.AddSource();
var sourceView = new SourceView { DataContext = sourceModel };
var modePicker = Children<ComboBox>(sourceView).First(box => box.ItemsSource is SourceMode[]);
modePicker.SelectedItem = SourceMode.Pulsed;
Check(sourceModel.Mode == SourceMode.Pulsed, "WPF-derived source mode selector updates the model");
var arrayToggle = Children<CheckBox>(sourceView).Last();
arrayToggle.IsChecked = true;
Check(sourceModel.Array is not null, "WPF-derived source array toggle creates the model array");
// Compose the actual scene used by the application, including image mode and a legacy relative path.
project.Scene.Mode = SceneMode.Image;
project.Scene.ImageData = bytes;
var composed = SceneImageLoader.Compose(project, null);
Check(composed is not null && composed.Width == 8 && composed.Height == 9
    && composed.Bgra[1] == 180, "Embedded images reach the simulation composition");
string folder = Path.Combine(Path.GetTempPath(), "wavedude-editor-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
try
{
    string imagePath = Path.Combine(folder, "scene.png");
    File.WriteAllBytes(imagePath, bytes);
    project.Scene.ImageData = null;
    project.Scene.Image = "scene.png";
    Check(SceneImageLoader.Compose(project, folder)!.Bgra.SequenceEqual(pixels.Bgra),
        "Legacy scene image paths resolve relative to the project");
    project.Scene.ImageData = bytes;
    project.Scene.ImagePath = "scene.png";
    using var watcher = new SceneImageWatch(Console.WriteLine);
    // A missing source file must not erase an embedded image.
    File.Delete(imagePath);
    watcher.Attach(project.Scene, folder);
    Check(project.Scene.ImageData!.SequenceEqual(bytes), "Missing source files preserve the embedded scene");
    using var replacement = new SKBitmap(10, 11);
    replacement.Erase(new SKColor(110, 0, 0));
    using var newEncoded = replacement.Encode(SKEncodedImageFormat.Png, 100);
    byte[] newBytes = newEncoded.ToArray();
    File.WriteAllBytes(imagePath, newBytes);
    watcher.Attach(project.Scene, folder);
    Check(project.Scene.ImageData!.SequenceEqual(newBytes), "Changed image files are re-embedded on project attachment");
    project.Scene.ImageData = bytes;
    watcher.Attach(project.Scene, folder, refresh: false);
    Check(project.Scene.ImageData!.SequenceEqual(bytes), "Undo attachment preserves the restored image bytes");
}
finally
{
    Directory.Delete(folder, recursive: true);
}
Console.WriteLine("All Avalonia editor checks passed.");

sealed class WatchAndHistory : IDisposable
{
    private readonly ProjectWatcher _watcher;
    public ProjectHistory History { get; }
    public int Changes { get; private set; }
    public WatchAndHistory(Project project)
    {
        History = new ProjectHistory(project);
        _watcher = new ProjectWatcher(project);
        _watcher.Changed += (_, _) => Changes++;
    }
    public void Dispose() => _watcher.Dispose();
}



sealed class WorkspaceCheckApp : App
{
    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        var desktop = (global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var window = (MainWindow)desktop.MainWindow!;
        window.Opened += async (_, _) =>
        {
            try
            {
                await Task.Delay(150);
                void Check(bool ok, string message)
                {
                    if (!ok) throw new InvalidOperationException(message);
                    Console.WriteLine("PASS " + message);
                }
                object? Field(string name) => typeof(MainWindow).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(window);
                void Invoke(string name, object? sender = null)
                {
                    var method = typeof(MainWindow).GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                    method.Invoke(window, method.GetParameters().Length == 0 ? null : [sender ?? window, new RoutedEventArgs()]);
                }
                var project = (Project)Field("_loadedProject")!;
                Check(project.Sources.Count == 1, "Startup is an editable project");
                window.FindControl<CheckBox>("AutoApply")!.IsChecked = false;
                var field = window.FindControl<Image>("FieldImage")!;
                var intensity = window.FindControl<Image>("IntensityImage")!;
                Check(field.TranslatePoint(new Point(), window)!.Value.X < intensity.TranslatePoint(new Point(), window)!.Value.X,
                    "Field and intensity previews are side by side");
                Invoke("AddPart_Click", new Button { Tag = "LensPart" });
                Check(project.Parts.Count == 1 && project.Parts[0] is LensPart, "Add lens creates and selects an editable part");
                Invoke("Duplicate_Click");
                Check(project.Parts.Count == 2, "Duplicate uses shared project logic");
                Invoke("Delete_Click");
                Check(project.Parts.Count == 1, "Delete removes the selected part");
                Invoke("Undo_Click");
                Check(project.Parts.Count == 2, "Object deletion can be undone");
                Invoke("Redo_Click");
                Check(project.Parts.Count == 1, "Object deletion can be redone");
                Invoke("AddSource_Click");
                Invoke("AddProbe_Click");
                Check(project.Sources.Count == 2, "Add source updates the project");
                for (int i = 0; i < 5; i++) Invoke("UpdateSimulation");
                var frames = (FrameStore)Field("_frames")!;
                var traces = (System.Collections.Generic.Dictionary<Probe, WaveDudeNext.Probes.ProbeTrace>)Field("_probeTraces")!;
                Check(traces.Count == 1 && traces.Values.Single().Series.Count == 10, "Probes record a sample for each simulation step");
                Check(frames.Count == 6, "Timeline records initial and simulated frames");
                window.FindControl<Slider>("TimelineSlider")!.Value = 2;
                Check((int)Field("_viewFrame")! == 2, "Timeline scrubs to an earlier stored frame");
                Check(window.FindControl<MenuItem>("ExamplesMenu")!.Items.Count > 0, "Bundled examples are available");
                // Attach every editor to the real styled properties pane to catch template-only binding errors.
                var pane = window.FindControl<WaveDudeNext.Avalonia.Panes.PropertiesPane>("PropertiesEditor")!;
                foreach (var type in typeof(OpticalPart).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(OpticalPart)) && !type.IsAbstract))
                {
                    var model = (OpticalPart)Activator.CreateInstance(type)!;
                    model.PrepareNew();
                    pane.SelectedObject = model;
                    await Task.Delay(20);
                    Check(pane.GetVisualDescendants().OfType<WaveDudeNext.Avalonia.Views.PartCommonView>().Any(), type.Name + " template renders");
                }
                foreach (var model in new object[] { project, project.Scene, project.Simulation, project.Display, project.Sources[0], project.Probes[0] })
                {
                    pane.SelectedObject = model;
                    await Task.Delay(20);
                }
                Invoke("AddScheme_Click");
                await Task.Delay(100);
                Check(window.GetVisualDescendants().OfType<ColorSchemeView>().Any(), "Colour scheme editor is reachable");
                var stopBox = window.GetVisualDescendants().OfType<ColorSchemeView>().Single().GetVisualDescendants().OfType<NumericUpDown>().First();
                Check(stopBox.GetVisualDescendants().OfType<TextBox>().Any(text => !string.IsNullOrEmpty(text.Text) && text.Bounds.Width > 20),
                    "Compact number editors show values beside the spinner buttons");
                window.FindControl<WaveDudeNext.Avalonia.Panes.ProjectTree>("ProjectTreeControl")!.SelectModel(project.Parts[0]);
                for (int i = 0; i < 60; i++) Invoke("UpdateSimulation");
                await Task.Delay(100);
                using var shot = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
                shot.Render(window);
                shot.Save("/tmp/wavedude-linux-workspace.png", global::Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                Check(EditorLog.Errors.Count == 0, "Workspace has no binding warnings or errors: " + string.Join("; ", EditorLog.Errors));
                Console.WriteLine("Workspace smoke checks passed. Screenshot: /tmp/wavedude-linux-workspace.png");
                typeof(MainWindow).GetField("_allowClose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(window, true);
                window.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Environment.ExitCode = 1;
                desktop.Shutdown(1);
            }
        };
    }
}

sealed class EditorLog : global::Avalonia.Logging.ILogSink
{
    public static readonly List<string> Errors = [];
    public bool IsEnabled(global::Avalonia.Logging.LogEventLevel level, string area) => area == "Binding" && level >= global::Avalonia.Logging.LogEventLevel.Warning;
    public void Log(global::Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) => Errors.Add(messageTemplate);
    public void Log(global::Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
        Errors.Add(messageTemplate + " " + string.Join(" | ", propertyValues));
}
