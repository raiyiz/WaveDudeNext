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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia;

public partial class MainWindow
{
    private static string PreferencesPath => Environment.GetEnvironmentVariable("WAVEDUDE_DESKTOP_SETTINGS") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WaveDudeNext", "desktop.json");
    private sealed class DesktopPreferences
    {
        public DesktopPreferences() { }
        public List<string> Recent { get; set; } = new();
        public double Width { get; set; } = 1500;
        public double Height { get; set; } = 900;
        public Dictionary<string, PanePlacement> Panes { get; set; } = new();
        public Dictionary<string, int> ProbePaneIds { get; set; } = new();
        public Docking.DockLayout? DockLayout { get; set; }
        public Controls.PreviewOverlays FieldOverlays { get; set; } = new();
        public Controls.PreviewOverlays IntensityOverlays { get; set; } = new();
        public string Quality { get; set; } = "Full";
        public bool PreviewStacked { get; set; }
        public bool TabbedPreviews { get; set; }
        public string Theme { get; set; } = "Dark";
        public string Language { get; set; } = "EN";
        public double TreeWidth { get; set; } = 240;
        public double PropertiesWidth { get; set; } = 340;
        public double Zoom { get; set; } = 1;
    }
    private sealed class PanePlacement
    {
        public PanePlacement() { }
        public bool Floating { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public double Width { get; set; } = 650;
        public double Height { get; set; } = 650;
    }
    private DesktopPreferences _desktop = new();
    private void InitializeDesktop()
    {
        try { if (File.Exists(PreferencesPath)) _desktop = JsonSerializer.Deserialize<DesktopPreferences>(File.ReadAllText(PreferencesPath)) ?? new(); }
        catch (Exception ex) { StatusText.Text = "Could not read workspace settings: " + ex.Message; }
        _desktop.FieldOverlays ??= new(); _desktop.IntensityOverlays ??= new();
        _desktop.ProbePaneIds ??= new();
        _desktop.Panes ??= new();
        _desktop.Recent ??= new();
        Width = Math.Clamp(_desktop.Width, MinWidth, 5000);
        Height = Math.Clamp(_desktop.Height, MinHeight, 3000);
        ApplyAppearance();
        ActualThemeVariantChanged += (_, _) => { if (_desktop.Theme == "Default") ApplyAppearance(); };
        ChooseQuality();
        InitializeDocking();
        RefreshRecent();
        Closed += (_, _) => { _desktop.Width = Width; _desktop.Height = Height; SaveDesktop(); };
    }
    private void SaveDesktop()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
            string temporary = PreferencesPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(_desktop));
            File.Move(temporary, PreferencesPath, true);
        }
        catch (Exception ex) { StatusText.Text = "Could not save workspace settings: " + ex.Message; }
    }
    private void RememberProject(string path)
    {
        path = Path.GetFullPath(path);
        _desktop.Recent.RemoveAll(p => p == path);
        _desktop.Recent.Insert(0, path);
        _desktop.Recent = _desktop.Recent.Take(12).ToList();
        SaveDesktop();
        RefreshRecent();
    }
    private void RefreshRecent()
    {
        RecentMenu.Items.Clear();
        foreach (string path in _desktop.Recent.ToArray())
        {
            var item = new MenuItem { Header = Path.GetFileName(path), Icon = new Controls.IconView { Drawing = "Project" } };
            ToolTip.SetTip(item, path);
            item.Click += async (_, _) =>
            {
                if (!await ConfirmProjectReplacement()) return;
                try { InstallProject(Project.Load(path), path); }
                catch (Exception ex) { StatusText.Text = "Could not open recent project: " + ex.Message; }
            };
            RecentMenu.Items.Add(item);
        }
        RecentMenu.IsEnabled = _desktop.Recent.Count > 0;
    }
    private async void Copy_Click(object? sender, RoutedEventArgs e) => await CopyObject(false);
    private async void Cut_Click(object? sender, RoutedEventArgs e) => await CopyObject(true);
    private async Task CopyObject(bool cut)
    {
        if (_loadedProject is not { } project || _selectedObject is not { } selected || Clipboard is null) return;
        if (!ProjectClipboard.CanCopy(project, selected) || (cut && selected is not WaveDudeNext.Model.INamed)) return;
        try
        {
            await Clipboard.SetTextAsync(ProjectClipboard.Write(project, _selection.Where(item => ProjectClipboard.CanCopy(project, item) && (!cut || item is WaveDudeNext.Model.INamed)).ToArray()));
            if (cut && ReferenceEquals(_loadedProject, project) && ReferenceEquals(_selectedObject, selected)) Delete_Click(this, new());
        }
        catch (Exception ex) { StatusText.Text = "Clipboard failed: " + ex.Message; }
    }
    private async void Paste_Click(object? sender, RoutedEventArgs e)
    {
        if (_loadedProject is not { } project || Clipboard is null) return;
        try
        {
            string? text = await Clipboard.TryGetTextAsync();
            if (!ReferenceEquals(project, _loadedProject)) return;
            var clip = text is null ? null : ProjectClipboard.Read(text);
            if (clip is null || !clip.CanPaste(project, _selectedObject)) { StatusText.Text = UiText.Text("The clipboard contains no compatible project objects."); return; }
            var pasted = ProjectClipboard.Paste(project, clip, _selectedObject);
            if (pasted.Count > 0) FinishObjectEdit(pasted[^1]);
        }
        catch (Exception ex) { StatusText.Text = "Could not paste: " + ex.Message; }
    }
    private async void ImportClassic_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = UiText.Text("Import classic Wave Dude configuration.json"), AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Classic project") { Patterns = new[] { "*.json" } } }
        });
        if (files.Count == 0 || !await ConfirmProjectReplacement()) return;
        try
        {
            string path = files[0].TryGetLocalPath() ?? throw new NotSupportedException("Select a local configuration file.");
            var notes = new List<string>();
            var project = LegacyProject.Import(path, notes, out string name);
            InstallProject(project, null);
            _hasPendingChanges = true;
            await ShowText("Classic project imported", "Imported " + name + ". Save As to create a .wdproj file.\n\n" + string.Join("\n", notes));
        }
        catch (Exception ex) { StatusText.Text = "Import failed: " + ex.Message; }
    }
    private Task ShowText(string title, string text)
    {
        var dialog = new Window { Title = UiText.Text(title), Width = 850, Height = 650, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var close = new Button { Content = Controls.IconView.Label("Exit", UiText.Text("Close")), HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => dialog.Close();
        var panel = new DockPanel { Margin = new Thickness(16), LastChildFill = true };
        DockPanel.SetDock(close, Dock.Bottom);
        panel.Children.Add(close);
        panel.Children.Add(new TextBox { Text = text, IsReadOnly = true, AcceptsReturn = true, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        dialog.Content = panel;
        return ShowWorkspaceDialog(dialog);
    }
    private async void Manual_Click(object? sender, RoutedEventArgs e)
    {
        try { await ShowText("WaveDude Next manual", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MANUAL.md"))); }
        catch (Exception ex) { StatusText.Text = "Could not open manual: " + ex.Message; }
    }
    private async void Diagnostics_Click(object? sender, RoutedEventArgs e) => await ShowText("About / diagnostics",
        $"WaveDude Next {typeof(Project).Assembly.GetName().Version}\n\n{RuntimeInformation.OSDescription}\n{RuntimeInformation.FrameworkDescription}\n{RuntimeInformation.ProcessArchitecture}\n\nEngine: {_engine.Description}\nCanvas: {_engine.Width} × {_engine.Height}\nFrame count: {_frames?.Count ?? 0}\nManaged memory: {GC.GetTotalMemory(false) / 1048576:N0} MiB\n\nGPL-3.0-or-later\nAlexander Graef; based on Wave Dude 2D by Jeroen Vleggaar and WaveSimulator2D by 0x23.");
}
