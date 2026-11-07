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
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace WaveDudeNext.Avalonia.Docking;

/// <summary>A split/tab workspace shared by the main window and its floating windows. UI thread only.</summary>
public sealed partial class DockWorkspace : Grid
{
    private sealed record Pane(string Id, string Title, Control Content);
    private sealed class Floating
    {
        public DockNode? Root;
        public Window Window = null!;
        public ContentControl Host = new();
        public LayoutTransformControl Scale = new();
    }
    private sealed record GroupVisual(DockNode Node, Grid Grid, Border Hint);
    private readonly Window _owner;
    private readonly Dictionary<string, Pane> _panes = new();
    private readonly List<Floating> _floating = new();
    private readonly List<GroupVisual> _groups = new();
    private readonly List<(DockNode Node, Grid Grid)> _splits = new();
    private readonly List<TabItem> _tabs = new();
    private readonly ContentControl _main = new();
    private DockNode? _root;
    private bool _rebuilding;
    private bool _closing;
    private double _uiScale = 1;
    public void SetScale(double value)
    {
        _uiScale = Math.Clamp(value, 0.8, 1.5);
        foreach (var floating in _floating) floating.Scale.LayoutTransform = new ScaleTransform(_uiScale, _uiScale);
    }
    public event Action? LayoutChanged;
    public event Action<string>? DragStatusChanged;
    public IReadOnlyList<Window> FloatingWindows => _floating.Select(f => f.Window).ToArray();
    public DockWorkspace(Window owner) { _owner = owner; Children.Add(_main); owner.AddHandler(KeyDownEvent, OnDragKey, RoutingStrategies.Tunnel); owner.Opened += (_, _) => { foreach (var floating in _floating) if (!floating.Window.IsVisible) floating.Window.Show(owner); }; }
    public void Register(string id, string title, Control content)
    {
        if (_panes.TryGetValue(id, out var old) && ReferenceEquals(old.Content, content))
        { if (old.Title != title) { _panes[id] = old with { Title = title }; Rebuild(); } return; }
        _panes[id] = new(id, title, content);
    }
    public void Remove(string id)
    {
        if (!_panes.ContainsKey(id)) return;
        CaptureSizes(); DetachContent(); RemoveFromLayouts(id); _panes.Remove(id); Rebuild(); Changed();
    }
    public bool Contains(string id) => _panes.ContainsKey(id);
    public bool IsPaneVisible(string id) => AllRoots().Any(root => FindGroup(root, id) is not null);
    public bool? AreStacked(string first, string second)
    {
        foreach (var root in AllRoots())
        {
            var a = FindGroup(root, first); var b = FindGroup(root, second);
            if (a is null || b is null || ReferenceEquals(a, b)) continue;
            var node = root!;
            while (node.IsSplit)
            {
                if (HasNode(node.First, a) && HasNode(node.First, b)) node = node.First!;
                else if (HasNode(node.Second, a) && HasNode(node.Second, b)) node = node.Second!;
                else return node.Vertical;
            }
        }
        return null;
    }
    public void Show(string id)
    {
        if (!_panes.ContainsKey(id)) return;
        var group = AllRoots().Select(root => FindGroup(root, id)).FirstOrDefault(g => g is not null);
        if (group is not null) { group.Active = id; Rebuild(); return; }
        if (_root is null) _root = DockNode.Group(id);
        else { var first = FirstGroup(_root); first.Items.Add(id); first.Active = id; }
        Rebuild(); Changed();
    }
    public void Hide(string id) { CaptureSizes(); RemoveFromLayouts(id); Rebuild(); Changed(); }
    public void Reset(bool tabbed = false)
    {
        CaptureSizes(); DetachContent(); CloseAllFloating();
        var previews = tabbed ? DockNode.Group("Field", "Intensity") : DockNode.Split(DockNode.Group("Field"), DockNode.Group("Intensity"));
        _root = DockNode.Split(DockNode.Group("Tree"), DockNode.Split(previews, DockNode.Group("Properties"), ratio: 0.72), ratio: 0.16);
        Rebuild(); Changed();
    }
    public void Dock(string id, string? targetId, DockSide side)
    {
        if (!_panes.ContainsKey(id)) return;
        CaptureSizes();
        DockNode? target = targetId is null ? _root : AllRoots().Select(root => FindGroup(root, targetId)).FirstOrDefault(g => g is not null);
        if (target is not null && target.Items.Count == 1 && target.Items[0] == id) return;
        RemoveFromLayouts(id);
        if (targetId is null) target = _root;
        if (target is null || !AllRoots().Any(root => HasNode(root, target)))
        { _root = _root is null ? DockNode.Group(id) : DockNode.Split(_root, DockNode.Group(id)); }
        else if (side == DockSide.Tab)
        { var group = FirstGroup(target); group.Items.Add(id); group.Active = id; }
        else
        {
            var added = DockNode.Group(id);
            bool before = side is DockSide.Left or DockSide.Top;
            var split = DockNode.Split(before ? added : target, before ? target : added, side is DockSide.Top or DockSide.Bottom, before ? 0.35 : 0.65);
            _root = Replace(_root, target, split);
            foreach (var floating in _floating) floating.Root = Replace(floating.Root, target, split);
        }
        Rebuild(); Changed();
    }
    public void Float(string id, PixelPoint? position = null)
    {
        if (!_panes.ContainsKey(id)) return;
        CaptureSizes();
        var pane = _panes[id];
        double width = Math.Clamp(pane.Content.Bounds.Width, 320, 1200), height = Math.Clamp(pane.Content.Bounds.Height, 240, 1000);
        RemoveFromLayouts(id);
        CreateFloating(new FloatingDockLayout { Root = DockNode.Group(id), X = position?.X ?? _owner.Position.X + 80, Y = position?.Y ?? _owner.Position.Y + 80, Width = width, Height = height });
        Rebuild(); Changed();
    }
    public void DockAll()
    {
        CaptureSizes();
        var ids = _floating.SelectMany(f => Ids(f.Root)).ToArray();
        foreach (var floating in _floating) floating.Root = null;
        foreach (string id in ids)
        {
            if (_root is null) _root = DockNode.Group(id);
            else _root = DockNode.Split(_root, DockNode.Group(id));
        }
        Rebuild(); Changed();
    }
    public DockLayout CaptureLayout()
    {
        CaptureSizes();
        return new DockLayout { Root = Copy(_root), Hidden = _panes.Keys.Where(id => !IsPaneVisible(id)).ToList(), Floating = _floating.Where(f => f.Root is not null).Select(f => new FloatingDockLayout
        { Root = Copy(f.Root), X = f.Window.Position.X, Y = f.Window.Position.Y, Width = f.Window.Width, Height = f.Window.Height }).ToList() };
    }
    public void Restore(DockLayout layout)
    {
        DetachContent(); CloseAllFloating();
        var seen = new HashSet<string>();
        _root = Clean(layout.Root, seen, 0);
        foreach (var saved in layout.Floating ?? new())
        {
            var root = Clean(saved.Root, seen, 0);
            if (root is not null) CreateFloating(new FloatingDockLayout { Root = root, X = saved.X, Y = saved.Y, Width = saved.Width, Height = saved.Height });
        }
        // Main panes are recoverable even after loading an old or partially corrupt layout.
        foreach (string id in new[] { "Tree", "Field", "Intensity", "Properties" })
            if (_panes.ContainsKey(id) && !seen.Contains(id) && !(layout.Hidden?.Contains(id) ?? false))
                _root = _root is null ? DockNode.Group(id) : DockNode.Split(_root, DockNode.Group(id));
        Rebuild();
    }
    public void CloseWindows()
    {
        _closing = true; DetachContent(); CloseAllFloating();
    }
    private void CreateFloating(FloatingDockLayout saved)
    {
        var floating = new Floating { Root = saved.Root };
        floating.Scale.Child = floating.Host; floating.Scale.LayoutTransform = new ScaleTransform(_uiScale, _uiScale);
        var window = new Window { Title = "WaveDude Next", Width = double.IsFinite(saved.Width) ? Math.Clamp(saved.Width, 240, 3000) : 640,
            Height = double.IsFinite(saved.Height) ? Math.Clamp(saved.Height, 180, 2000) : 480, MinWidth = 180, MinHeight = 140,
            RequestedThemeVariant = _owner.RequestedThemeVariant, Background = Brush("WorkspaceBackground"), Foreground = Brush("WorkspaceForeground"), Content = floating.Scale };
        floating.Window = window;
        var location = new PixelPoint(saved.X, saved.Y);
        if (_owner.Screens.ScreenFromPoint(location) is not null) window.Position = location;
        window.Closing += (_, args) =>
        {
            if (_rebuilding || _closing) return;
            args.Cancel = true;
            var ids = Ids(floating.Root).ToArray();
            floating.Root = null;
            foreach (string id in ids) { if (_root is null) _root = DockNode.Group(id); else FirstGroup(_root).Items.Add(id); }
            Rebuild(); Changed();
        };
        window.AddHandler(KeyDownEvent, OnDragKey, RoutingStrategies.Tunnel);
        _floating.Add(floating);
    }
    private void Rebuild()
    {
        CaptureSizes();
        _rebuilding = true;
        try
        {
            DetachContent();
            for (int i = _floating.Count - 1; i >= 0; i--)
                if (_floating[i].Root is null) { _floating[i].Window.Close(); _floating.RemoveAt(i); }
            _main.Content = _root is null ? new TextBlock { Text = UiText.Text("Use View > Panes to restore a pane."), Margin = new Thickness(20) } : Build(_root);
            foreach (var floating in _floating)
            {
                floating.Host.Content = Build(floating.Root!);
                floating.Window.Title = "WaveDude Next — " + string.Join(" / ", Ids(floating.Root).Select(id => _panes[id].Title));
                if (_owner.IsVisible && !floating.Window.IsVisible) floating.Window.Show(_owner);
            }
        }
        finally { _rebuilding = false; }
    }
    private Control Build(DockNode node)
    {
        if (node.IsSplit)
        {
            var grid = new Grid();
            double ratio = double.IsFinite(node.Ratio) ? Math.Clamp(node.Ratio, 0.08, 0.92) : 0.5;
            if (node.Vertical) grid.RowDefinitions = new RowDefinitions { new(new GridLength(ratio, GridUnitType.Star)), new(new GridLength(5)), new(new GridLength(1 - ratio, GridUnitType.Star)) };
            else grid.ColumnDefinitions = new ColumnDefinitions { new(new GridLength(ratio, GridUnitType.Star)), new(new GridLength(5)), new(new GridLength(1 - ratio, GridUnitType.Star)) };
            var first = Build(node.First!); var second = Build(node.Second!);
            var splitter = new GridSplitter { Background = Brush("WorkspaceBorder"), ResizeDirection = node.Vertical ? GridResizeDirection.Rows : GridResizeDirection.Columns,
                HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, Focusable = true };
            AutomationProperties.SetName(splitter, UiText.Text("Resize docked panes"));
            if (node.Vertical) { Grid.SetRow(splitter, 1); Grid.SetRow(second, 2); }
            else { Grid.SetColumn(splitter, 1); Grid.SetColumn(second, 2); }
            splitter.PointerReleased += (_, _) => { CaptureSizes(); Changed(); };
            grid.Children.Add(first); grid.Children.Add(splitter); grid.Children.Add(second);
            _splits.Add((node, grid));
            return grid;
        }
        var group = new Grid { Background = Brush("WorkspacePane") };
        var tabs = new TabControl();
        foreach (string id in node.Items)
        {
            var pane = _panes[id];
            var title = new TextBlock { Text = pane.Title, FontSize = 12, FontWeight = FontWeight.SemiBold, Padding = new Thickness(4, 3), Cursor = new Cursor(StandardCursorType.SizeAll) };
            ToolTip.SetTip(title, UiText.Text("Drag to dock; double-click to float. Right-click for docking commands."));
            var item = new TabItem { Header = title, Content = pane.Content, MinHeight = 28, Padding = new Thickness(6, 2) };
            AutomationProperties.SetName(item, pane.Title);
            AutomationProperties.SetAutomationId(item, "DockTab:" + id);
            WireHeader(title, id);
            title.ContextMenu = PaneMenu(id);
            _tabs.Add(item); tabs.Items.Add(item);
            if (id == node.Active) tabs.SelectedItem = item;
        }
        if (tabs.SelectedIndex < 0) tabs.SelectedIndex = 0;
        tabs.SelectionChanged += (_, _) => { int index = tabs.SelectedIndex; if (!_rebuilding && index >= 0 && index < node.Items.Count) { node.Active = node.Items[index]; Changed(); } };
        group.Children.Add(tabs);
        var hint = new Border { IsVisible = false, IsHitTestVisible = false, Background = new SolidColorBrush(Color.FromArgb(100, 0, 150, 255)), BorderBrush = Brushes.DeepSkyBlue, BorderThickness = new Thickness(2) };
        group.Children.Add(hint); _groups.Add(new(node, group, hint));
        return group;
    }
    private ContextMenu PaneMenu(string id)
    {
        var menu = new ContextMenu();
        foreach (var (label, side) in new[] { (UiText.Text("Dock left"), DockSide.Left), (UiText.Text("Dock right"), DockSide.Right), (UiText.Text("Dock above"), DockSide.Top), (UiText.Text("Dock below"), DockSide.Bottom), (UiText.Text("Dock as tab"), DockSide.Tab) })
        {
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => Dock(id, Ids(_root).FirstOrDefault(key => key != id), side);
            menu.Items.Add(item);
        }
        var floatItem = new MenuItem { Header = UiText.Text("Float pane") }; floatItem.Click += (_, _) => Float(id); menu.Items.Add(floatItem);
        var hide = new MenuItem { Header = UiText.Text("Hide pane") }; hide.Click += (_, _) => Hide(id); menu.Items.Add(hide);
        return menu;
    }
    private IBrush Brush(string key) => _owner.TryFindResource(key, out var value) && value is IBrush brush ? brush : Brushes.DimGray;
    private void CaptureSizes()
    {
        foreach (var (node, grid) in _splits)
        {
            double first = node.Vertical ? grid.RowDefinitions[0].ActualHeight : grid.ColumnDefinitions[0].ActualWidth;
            double last = node.Vertical ? grid.RowDefinitions[2].ActualHeight : grid.ColumnDefinitions[2].ActualWidth;
            if (first + last > 0) node.Ratio = Math.Clamp(first / (first + last), 0.08, 0.92);
        }
    }
    private void DetachContent()
    {
        foreach (var tab in _tabs) tab.Content = null;
        _tabs.Clear(); _main.Content = null;
        foreach (var floating in _floating) floating.Host.Content = null;
        _groups.Clear(); _splits.Clear();
    }
    private void CloseAllFloating()
    {
        bool previous = _rebuilding; _rebuilding = true;
        foreach (var floating in _floating.ToArray()) floating.Window.Close();
        _floating.Clear(); _rebuilding = previous;
    }
    private void Changed() { if (!_rebuilding && !_closing) LayoutChanged?.Invoke(); }
    private IEnumerable<DockNode?> AllRoots() => new[] { _root }.Concat(_floating.Select(f => f.Root));
    private void RemoveFromLayouts(string id)
    { _root = RemoveId(_root, id); foreach (var floating in _floating) floating.Root = RemoveId(floating.Root, id); }
    private static DockNode? RemoveId(DockNode? node, string id)
    {
        if (node is null) return null;
        if (!node.IsSplit) { node.Items.Remove(id); if (node.Active == id) node.Active = node.Items.FirstOrDefault(); return node.Items.Count == 0 ? null : node; }
        node.First = RemoveId(node.First, id); node.Second = RemoveId(node.Second, id);
        return node.First is null ? node.Second : node.Second is null ? node.First : node;
    }
    private static DockNode? Replace(DockNode? node, DockNode target, DockNode replacement)
    {
        if (ReferenceEquals(node, target)) return replacement;
        if (node is not null && node.IsSplit) { node.First = Replace(node.First, target, replacement); node.Second = Replace(node.Second, target, replacement); }
        return node;
    }
    private static DockNode? FindGroup(DockNode? node, string id) => node is null ? null : node.Items.Contains(id) ? node : FindGroup(node.First, id) ?? FindGroup(node.Second, id);
    private static DockNode FirstGroup(DockNode node) => node.IsSplit ? FirstGroup(node.First ?? node.Second!) : node;
    private static bool HasNode(DockNode? root, DockNode node) => root is not null && (ReferenceEquals(root, node) || HasNode(root.First, node) || HasNode(root.Second, node));
    private static IEnumerable<string> Ids(DockNode? node) => node is null ? Array.Empty<string>() : node.IsSplit ? Ids(node.First).Concat(Ids(node.Second)) : node.Items;
    private static DockNode? Copy(DockNode? node) => node is null ? null : new() { Items = new(node.Items), Active = node.Active, Vertical = node.Vertical, Ratio = node.Ratio, First = Copy(node.First), Second = Copy(node.Second) };
    private DockNode? Clean(DockNode? node, HashSet<string> seen, int depth)
    {
        if (node is null || depth > 40) return null;
        if (node.IsSplit)
        {
            var first = Clean(node.First, seen, depth + 1); var second = Clean(node.Second, seen, depth + 1);
            return first is null ? second : second is null ? first : DockNode.Split(first, second, node.Vertical, double.IsFinite(node.Ratio) ? Math.Clamp(node.Ratio, 0.08, 0.92) : 0.5);
        }
        var ids = (node.Items ?? new()).Where(id => _panes.ContainsKey(id) && seen.Add(id)).ToArray();
        return ids.Length == 0 ? null : new DockNode { Items = new(ids), Active = ids.Contains(node.Active) ? node.Active : ids[0] };
    }
}
