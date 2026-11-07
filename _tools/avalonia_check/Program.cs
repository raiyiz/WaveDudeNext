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
using Avalonia.Input;
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

Environment.SetEnvironmentVariable("WAVEDUDE_DESKTOP_SETTINGS", Path.Combine(Path.GetTempPath(), "wavedude-check-" + Guid.NewGuid().ToString("N"), "desktop.json"));

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
                Invoke("PreviewQuality_Click", new MenuItem { Tag = "Full" });
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
                var overlayCanvas = window.FindControl<SceneCanvas>("FieldCanvas")!;
                var otherCanvas = window.FindControl<SceneCanvas>("IntensityCanvas")!;
                window.FindControl<WaveDudeNext.Avalonia.Panes.ProjectTree>("ProjectTreeControl")!.SelectModel(project.Parts[0]);
                await Task.Delay(40);
                bool HasOverlayPixels(SceneCanvas canvas)
                {
                    using var picture = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height));
                    picture.Render(canvas);
                    using var png = new MemoryStream(); picture.Save(png, global::Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                    var decoded = SceneImageLoader.Load(png.ToArray());
                    return Enumerable.Range(0, decoded.Width * decoded.Height).Any(index => decoded.Bgra[index * 4 + 3] != 0);
                }
                Check(HasOverlayPixels(overlayCanvas), "Preview drawings initially include selected geometry and probe graphs");
                var showDrawings = window.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.ToggleButton>()
                    .Single(button => global::Avalonia.Automation.AutomationProperties.GetAutomationId(button) == "FieldDrawingsToggle");
                var samplesBefore = new Half[frames.Width * frames.Height]; frames.CopyLayer(0, FrameLayer.Field, samplesBefore);
                int historyBefore = ((ProjectHistory)Field("_projectHistory")!).Count;
                bool pendingBefore = (bool)Field("_hasPendingChanges")!;
                showDrawings.IsChecked = false;
                Check(!HasOverlayPixels(overlayCanvas), "Hide drawings removes every vector overlay, including selected-object handles and labels");
                Check(otherCanvas.Overlays.Drawings && ReferenceEquals(field.Source, window.FindControl<Image>("FieldImage")!.Source), "Hiding field drawings keeps the simulation image and intensity visibility independent");
                var samplesAfter = new Half[samplesBefore.Length]; frames.CopyLayer(0, FrameLayer.Field, samplesAfter);
                Check(samplesBefore.SequenceEqual(samplesAfter) && frames.Count == 6 && project.Sources.All(source => source.Enabled) && project.Probes.All(probe => probe.Active), "Overlay visibility preserves numerical frames, enabled sources and probe recording");
                Check(((ProjectHistory)Field("_projectHistory")!).Count == historyBefore && (bool)Field("_hasPendingChanges")! == pendingBefore, "Overlay visibility does not dirty the project or create an undo step");
                showDrawings.IsChecked = true;
                Check(HasOverlayPixels(overlayCanvas), "Drawings can be restored after hiding everything");
                var overlayOptions = window.GetVisualDescendants().OfType<Button>()
                    .Single(button => global::Avalonia.Automation.AutomationProperties.GetAutomationId(button) == "FieldOverlayOptions");
                overlayOptions.Flyout!.ShowAt(overlayOptions); await Task.Delay(40);
                var optionsPanel = (StackPanel)((ScrollViewer)((Flyout)overlayOptions.Flyout).Content!).Content!;
                var category = optionsPanel.Children.OfType<CheckBox>().Single(check => (string?)check.Content == "Source markers");
                category.IsChecked = false;
                Check(!overlayCanvas.Overlays.ShowsObject(project.Sources[0]) && project.Sources[0].Enabled, "Source-marker visibility does not disable its emitter");
                category.IsChecked = true;
                var individual = optionsPanel.Children.OfType<CheckBox>().Single(check => (string?)check.Content == "Source: " + project.Sources[0].Name);
                individual.IsChecked = false;
                Check(!overlayCanvas.Overlays.ShowsObject(project.Sources[0]) && overlayCanvas.Overlays.ShowsObject(project.Sources[1]), "Individual drawings can be hidden independently");
                individual.IsChecked = true; overlayOptions.Flyout.Hide();
                var visibilityJson = System.Text.Json.JsonSerializer.Serialize(overlayCanvas.Overlays);
                Check(System.Text.Json.JsonSerializer.Deserialize<PreviewOverlays>(visibilityJson) is { Drawings: true, Sources: true }, "Preview overlay category settings can be persisted");
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
                Check(window.GetVisualDescendants().OfType<WaveDudeNext.Avalonia.Controls.IconView>().Count() >= 15, "Toolbar and playback icons render as vector controls");
                Invoke("MarkIn_Click");
                Check((int)Field("_playIn")! == (int)Field("_viewFrame")!, "Playback in point follows the current frame");
                Invoke("ClearRange_Click");
                Check((int)Field("_playIn")! == 0 && Field("_playOut") is null, "Playback range resets to the complete recording");
                var dock = (WaveDudeNext.Avalonia.Docking.DockWorkspace)Field("_dock")!;
                Invoke("TabbedPreviews_Click");
                Check(dock.CaptureLayout().Root is not null, "Previews can be tabbed");
                Invoke("SideBySide_Click"); await Task.Delay(60);
                Check(field.IsAttachedToVisualTree(), "Tabbed previews dock back side by side");
                Invoke("FloatPane_Click", new MenuItem { Tag = "Properties" }); await Task.Delay(60);
                Check(dock.FloatingWindows.Count == 1 && pane.IsAttachedToVisualTree(), "Properties pane floats without losing its content");
                Invoke("DockAll_Click"); await Task.Delay(60);
                Check(dock.FloatingWindows.Count == 0 && pane.IsAttachedToVisualTree(), "Floating pane docks back without losing its content");
                dock.Dock("Properties", "Field", WaveDudeNext.Avalonia.Docking.DockSide.Bottom);
                var savedDock = dock.CaptureLayout();
                dock.Reset(); dock.Restore(savedDock); await Task.Delay(60);
                Check(dock.CaptureLayout().Root is not null && pane.IsAttachedToVisualTree(), "Nested dock layout survives save/restore");
                Invoke("ResetLayout_Click");
                if (WaveDudeNext.Avalonia.Rendering.FfmpegSink.FindExecutable("xdotool") is { } xdotool)
                {
                    async Task Mouse(params string[] arguments)
                    {
                        var info = new System.Diagnostics.ProcessStartInfo(xdotool) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                        foreach (string argument in arguments) info.ArgumentList.Add(argument);
                        using var process = System.Diagnostics.Process.Start(info)!;
                        await process.WaitForExitAsync(); await Task.Delay(80);
                    }
                    async Task Drag(Control header, global::Avalonia.PixelPoint to)
                    {
                        var from = header.PointToScreen(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2));
                        await Mouse("mousemove", "--sync", from.X.ToString(), from.Y.ToString(), "mousedown", "1");
                        await Mouse("mousemove", "--sync", to.X.ToString(), to.Y.ToString());
                        await Mouse("mouseup", "1");
                    }
                    Control Header(string id, Window? floating = null) => (Control)(floating ?? window).GetVisualDescendants().OfType<TabItem>()
                        .Single(tab => global::Avalonia.Automation.AutomationProperties.GetAutomationId(tab) == "DockTab:" + id).Header!;
                    WaveDudeNext.Avalonia.Docking.DockNode? Group(WaveDudeNext.Avalonia.Docking.DockNode? node, string id) => node is null ? null : node.Items.Contains(id) ? node : Group(node.First, id) ?? Group(node.Second, id);
                    await Task.Delay(80);
                    var targetHost = window.FindControl<Border>("FieldHost")!;
                    var center = targetHost.PointToScreen(new Point(targetHost.Bounds.Width / 2, targetHost.Bounds.Height / 2));
                    var cancelHeader = Header("Properties");
                    var cancelStart = cancelHeader.PointToScreen(new Point(10, 8));
                    await Mouse("mousemove", "--sync", cancelStart.X.ToString(), cancelStart.Y.ToString(), "mousedown", "1");
                    await Mouse("mousemove", "--sync", center.X.ToString(), center.Y.ToString(), "key", "Escape", "mouseup", "1");
                    Check(!dock.IsDragging && Group(dock.CaptureLayout().Root, "Field") != Group(dock.CaptureLayout().Root, "Properties"), "Escape cancels pane dragging without changing its docking group");
                    await Drag(Header("Properties"), center);
                    Check(Group(dock.CaptureLayout().Root, "Field")!.Items.Contains("Properties"), "Mouse drag docks a pane into another pane's tabs");
                    dock.Reset(); await Task.Delay(100);
                    await Drag(Header("Properties"), window.PointToScreen(new Point(180, 60)));
                    Check(dock.FloatingWindows.Count == 1, "Mouse drag outside docking targets floats a pane");
                    await Drag(Header("Properties", dock.FloatingWindows[0]), targetHost.PointToScreen(new Point(8, targetHost.Bounds.Height / 2)));
                    Check(dock.FloatingWindows.Count == 0 && Group(dock.CaptureLayout().Root, "Field") != Group(dock.CaptureLayout().Root, "Properties"), "Floating pane mouse drag docks beside a target");
                    dock.Reset(); await Task.Delay(100);
                    var probeCanvas = window.FindControl<WaveDudeNext.Avalonia.Controls.SceneCanvas>("FieldCanvas")!;
                    var probe = project.Probes[0];
                    var graph = probeCanvas.GraphRect(probe);
                    var start = probeCanvas.PointToScreen(new Point(graph.X + 35, graph.Y + 8));
                    int oldX = probe.BoxX, oldY = probe.BoxY, pointX = probe.AX, pointY = probe.AY;
                    await Mouse("mousemove", "--sync", start.X.ToString(), start.Y.ToString(), "mousedown", "1");
                    await Mouse("mousemove", "--sync", (start.X - 25).ToString(), (start.Y - 20).ToString(), "mouseup", "1");
                    Check((probe.BoxX != oldX || probe.BoxY != oldY) && probe.AX == pointX && probe.AY == pointY, "Dragging a probe viewport moves the graph without moving the probe");
                    graph = probeCanvas.GraphRect(probe); start = probeCanvas.PointToScreen(graph.BottomRight - new Vector(3, 3));
                    await Mouse("mousemove", "--sync", start.X.ToString(), start.Y.ToString(), "mousedown", "1");
                    await Mouse("mousemove", "--sync", (start.X - 35).ToString(), (start.Y - 25).ToString(), "mouseup", "1");
                    Check(probe.BoxSize == ProbeBoxSize.Custom && probe.BoxWidth < Probe.DefaultBoxWidth, "Dragging a probe viewport corner resizes its graph");
                    typeof(MainWindow).GetMethod("OpenProbeViewport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, new object[] { probe });
                    await Task.Delay(100);
                    Check(dock.IsPaneVisible("Probe.0") && window.GetVisualDescendants().OfType<WaveDudeNext.Avalonia.Controls.ProbeViewport>().Any(), "Probe graphs can open as dockable viewports");
                    Check(!probeCanvas.Overlays.ShowsGraph(probe) && !otherCanvas.Overlays.ShowsGraph(probe), "Opening a separate probe viewport hides its graph over the simulation");
                    Check(Group(dock.CaptureLayout().Root, "Probe.0") != Group(dock.CaptureLayout().Root, "Intensity"), "Probe viewport receives its own layout space outside the simulation image");
                    typeof(MainWindow).GetMethod("ShowProbeOverSimulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, new object[] { probe });
                    Check(probeCanvas.Overlays.ShowsGraph(probe) && dock.IsPaneVisible("Probe.0"), "Canvas graph can be restored independently while its external viewport remains open");
                    typeof(MainWindow).GetMethod("HideProbeOverSimulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, new object[] { probe });
                    dock.Float("Probe.0"); await Task.Delay(80);
                    Check(dock.FloatingWindows.Count == 1 && dock.FloatingWindows[0].GetVisualDescendants().OfType<WaveDudeNext.Avalonia.Controls.ProbeViewport>().Any(), "Probe viewport can float independently");
                    var floatingJson = System.Text.Json.JsonSerializer.Serialize(dock.CaptureLayout());
                    dock.Restore(System.Text.Json.JsonSerializer.Deserialize<WaveDudeNext.Avalonia.Docking.DockLayout>(floatingJson)!); await Task.Delay(80);
                    Check(dock.FloatingWindows.Count == 1 && dock.IsPaneVisible("Probe.0"), "Floating probe viewport survives serialized layout restoration");
                    dock.Dock("Probe.0", "Intensity", WaveDudeNext.Avalonia.Docking.DockSide.Bottom); await Task.Delay(80);
                    Check(dock.FloatingWindows.Count == 0 && dock.IsPaneVisible("Probe.0"), "Probe viewport docks below another pane");
                    dock.Reset(); await Task.Delay(80);
                    var lens = (LensPart)project.Parts[0];
                    window.FindControl<WaveDudeNext.Avalonia.Panes.ProjectTree>("ProjectTreeControl")!.SelectModel(lens);
                    probeCanvas.Focus();
                    int oldDiameter = lens.Diameter;
                    probeCanvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Tab });
                    probeCanvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
                    Check(lens.Diameter > oldDiameter, "Keyboard geometry handles resize the selected lens");
                    probeCanvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
                    var canvasPeer = global::Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(probeCanvas)!;
                    Check(canvasPeer.GetName().Contains(lens.Name) && canvasPeer.GetHelpText().Length > 0, "Preview automation describes selection and keyboard editing");
                }
                Invoke("Theme_Click", new MenuItem { Tag = "Light" });
                Check(window.RequestedThemeVariant == global::Avalonia.Styling.ThemeVariant.Light, "Theme switching applies to the workspace");
                Invoke("Theme_Click", new MenuItem { Tag = "Default" });
                Check(global::Avalonia.Application.Current!.RequestedThemeVariant == global::Avalonia.Styling.ThemeVariant.Default, "System theme follows the platform rather than the application's old dark default");
                Invoke("Theme_Click", new MenuItem { Tag = "Dark" });
                pane.SelectedObject = project.Scene;
                Invoke("Language_Click", new MenuItem { Tag = "DE" }); await Task.Delay(50);
                Check(global::Avalonia.Application.Current!.TryFindResource("MenuFile", out var translated) && (string)translated! == "_Datei", "Language switching loads WPF German resources");
                Check(pane.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == "Breite"), "Editor labels use the selected language instead of local English copies");
                Check(pane.GetVisualDescendants().OfType<NumericUpDown>().Any(input => global::Avalonia.Automation.AutomationProperties.GetLabeledBy(input) is not null), "Numeric form editors expose their associated labels to accessibility tools");
                Check(global::Avalonia.Application.Current!.TryFindResource("Shell.Panes", out var paneLabel) && (string)paneLabel! == "Bereiche", "New docking shell labels are translated");
                Invoke("Language_Click", new MenuItem { Tag = "EN" });
                Invoke("UiZoom_Click", new MenuItem { Tag = "1.25" });
                Check(window.FindControl<LayoutTransformControl>("WorkspaceScale")!.LayoutTransform is global::Avalonia.Media.ScaleTransform scale && scale.ScaleX == 1.25, "UI scale can be changed");
                Invoke("UiZoom_Click", new MenuItem { Tag = "1" });
                var tree = window.FindControl<WaveDudeNext.Avalonia.Panes.ProjectTree>("ProjectTreeControl")!;
                tree.SelectModel(project.Parts[0]);
                int partCount = project.Parts.Count;
                Invoke("Copy_Click"); await Task.Delay(150);
                Invoke("Paste_Click"); await Task.Delay(150);
                Check(project.Parts.Count == partCount + 1, "System clipboard copies and pastes optical parts");
                Invoke("Undo_Click");
                Check(project.Parts.Count == partCount, "Clipboard paste is undoable");
                var background = (Task)typeof(MainWindow).GetMethod("UpdateBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, null)!;
                await background;
                Check((int)Field("_step")! > 0, "Background stepping publishes completed steps");
                Invoke("PreviewQuality_Click", new MenuItem { Tag = "Economy" });
                Invoke("UpdateSimulation");
                var reduced = (FrameStore)Field("_frames")!;
                Check(window.FindControl<Slider>("TimelineSlider")!.Maximum == 2 && reduced.Count == 2, "Economy timeline retains full frame numbering with repeated stored frames");
                window.FindControl<Slider>("TimelineSlider")!.Value = 1;
                Check((int)typeof(MainWindow).GetMethod("StoredFrameForTimeline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, new object[] { 1 })! == 0, "Missing timeline frames use the preceding stored frame");
                project.Simulation.Steps = 12;
                var stillMethod = typeof(MainWindow).GetMethod("RenderCurrentFrameForExport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                var still = await (Task<(int Width, int Height, int[] Pixels)?>)stillMethod.Invoke(window, new object[] { false })!;
                Check(still is { } fullStill && fullStill.Width == project.Scene.Width && fullStill.Height == project.Scene.Height, "Current-frame export reruns reduced previews at full resolution");
                Check(reduced.Width == project.Scene.Width / 2 && reduced.Format == WaveDudeNext.Engine.FrameFormat.Index, "Economy preview stores half-resolution 8-bit frames");
                Check(window.FindControl<Image>("FieldImage")!.Source!.Size.Width == reduced.Width, "Reduced preview bitmap matches the stored dimensions");
                Invoke("PreviewQuality_Click", new MenuItem { Tag = "Full" });
                project.Simulation.Steps = 12;
                var prepare = typeof(MainWindow).GetMethod("PrepareExportFrames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                using var fullExport = await (Task<FrameStore?>)prepare.Invoke(window, new object[] { true, "Export verification" })!;
                Check(fullExport is { Count: > 0, IntensityRoot: true } && fullExport.Width == project.Scene.Width, "Export rerun produces full-resolution half-float frames");
                string exportFolder = Path.Combine(Path.GetTempPath(), "wavedude-export-check-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(exportFolder);
                foreach (var exportFormat in new[] { WaveDudeNext.Export.ExportFormat.Png, WaveDudeNext.Export.ExportFormat.Jpeg, WaveDudeNext.Export.ExportFormat.ExrRoot, WaveDudeNext.Export.ExportFormat.Avi, WaveDudeNext.Export.ExportFormat.Mov, WaveDudeNext.Export.ExportFormat.MovPng, WaveDudeNext.Export.ExportFormat.Mpeg1 })
                {
                    string destination = Path.Combine(exportFolder, exportFormat + WaveDudeNext.Export.ExportFormats.Extension(exportFormat));
                    var exportLayer = new WaveDudeNext.Export.ExportLayer(FrameLayer.Field, WaveDudeNext.Rendering.ColorLut.Grey, 1, null, 0, null);
                    var exportTarget = new WaveDudeNext.Export.ExportTarget(exportLayer, destination,
                        () => WaveDudeNext.Export.ExportFormats.Open(exportFormat, destination, fullExport!.Width, fullExport.Height, 30, 85, 2));
                    var exportJob = new WaveDudeNext.Export.ExportJob(fullExport!, 0, 1, new[] { exportTarget }, null);
                    exportJob.Start(); await exportJob.Completion;
                    Check(exportJob.State == WaveDudeNext.Export.ExportState.Completed, exportFormat + " export writes recorded frames: " + exportJob.Error);
                }
                if (WaveDudeNext.Avalonia.Rendering.FfmpegSink.IsAvailable)
                {
                    string mp4 = Path.Combine(exportFolder, "recording.mp4");
                    using (var writer = new WaveDudeNext.Avalonia.Rendering.FfmpegSink(mp4, 16, 16, 30, 80))
                    { writer.Add(new int[256]); writer.Add(new int[256]); }
                    Check(new FileInfo(mp4).Length > 0, "ffmpeg writes a portable MP4 recording");
                }
                if (WaveDudeNext.Avalonia.Rendering.FfmpegSink.FindExecutable("convert") is { } converter)
                {
                    var conversion = new System.Diagnostics.ProcessStartInfo(converter) { RedirectStandardOutput = true, UseShellExecute = false };
                    foreach (string argument in new[] { "-size", "2x1", "xc:red", "tiff:-" }) conversion.ArgumentList.Add(argument);
                    using var process = System.Diagnostics.Process.Start(conversion)!;
                    using var tiff = new MemoryStream();
                    await process.StandardOutput.BaseStream.CopyToAsync(tiff); await process.WaitForExitAsync();
                    var decoded = SceneImageLoader.Load(tiff.ToArray());
                    Check(decoded.Width == 2 && decoded.Height == 1 && decoded.Bgra[2] == 255, "Optional ImageMagick decoder preserves TIFF scene pixels");
                }
                using var rawExport = await (Task<FrameStore?>)prepare.Invoke(window, new object[] { false, "Raw intensity verification" })!;
                Check(rawExport is { IntensityRoot: false, Count: > 0 }, "Raw OpenEXR rerun records intensity without square-root compression");
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
