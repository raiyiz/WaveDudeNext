using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using WaveDudeNext.Model;
using WaveDudeNext.Avalonia.Controls;
using WaveDudeNext.Rendering;

namespace WaveDudeNext.Avalonia.Panes;

public partial class ProjectTree : UserControl
{
    public event Action<object?>? SelectedModelChanged;
    public event Action<object[]>? SelectedModelsChanged;
    public event Action<string, object?>? CommandRequested;
    public object[] DraggedObjects { get; private set; } = Array.Empty<object>();
    private Project? _project;
    private bool _rebuilding;
    private PointerPressedEventArgs? _dragPress;
    private Point _dragOrigin;
    private static readonly FuncValueConverter<bool, double> EnabledOpacity = new(value => value ? 1 : 0.45);

    public ProjectTree()
    {
        InitializeComponent();
        RefreshCommandState();
        ProjectTreeView.AddHandler(PointerPressedEvent, (_, e) =>
        {
            // Buttons have their own click action; moving the pointer over one must never start an object drag.
            if (e.Source is Button or CheckBox || (e.Source as Control)?.FindAncestorOfType<Button>() is not null || (e.Source as Control)?.FindAncestorOfType<CheckBox>() is not null)
            { _dragPress = null; return; }
            _dragPress = e; _dragOrigin = e.GetPosition(ProjectTreeView);
        }, RoutingStrategies.Tunnel);
        ProjectTreeView.PointerMoved += async (_, e) =>
        {
            if (_dragPress is null || !e.GetCurrentPoint(ProjectTreeView).Properties.IsLeftButtonPressed || _project is null) return;
            var pointer = e.GetPosition(ProjectTreeView);
            if (Math.Abs(pointer.X - _dragOrigin.X) + Math.Abs(pointer.Y - _dragOrigin.Y) < 8) return;
            var press = _dragPress; _dragPress = null;
            var models = SelectedModels.Where(item => item is WaveDudeNext.Model.INamed).ToArray();
            if (models.Length == 0) return;
            var data = new DataTransfer(); data.Add(DataTransferItem.CreateText(ProjectClipboard.Write(_project, models)));
            DraggedObjects = models;
            try { await DragDrop.DoDragDropAsync(press, data, DragDropEffects.Copy | DragDropEffects.Move); }
            catch (Exception ex) { Console.WriteLine("Drag failed: " + ex.Message); }
            finally { DraggedObjects = Array.Empty<object>(); }
        };
        ProjectTreeView.SelectionChanged += (_, _) => { if (!_rebuilding) NotifySelection(); };
        ProjectTreeView.AddHandler(KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Tunnel);
    }
    public object? SelectedModel => (ProjectTreeView.SelectedItem as TreeViewItem)?.Tag;
    private object[] SelectedModels => ProjectTreeView.SelectedItems?.OfType<TreeViewItem>().Select(row => row.Tag).Where(item => item is not null).Cast<object>().ToArray() ?? Array.Empty<object>();
    private IEnumerable<TreeViewItem> Rows(IEnumerable? items = null)
    {
        foreach (var item in items ?? ProjectTreeView.Items)
            if (item is TreeViewItem row)
            {
                yield return row;
                foreach (var child in Rows(row.Items)) yield return child;
            }
    }
    public void SelectModel(object? model)
    {
        bool previous = _rebuilding; _rebuilding = true;
        try
        {
            ProjectTreeView.SelectedItems?.Clear();
            ProjectTreeView.SelectedItem = model is null ? null : Rows().FirstOrDefault(row => ReferenceEquals(row.Tag, model));
        }
        finally { _rebuilding = previous; }
        if (!_rebuilding) NotifySelection();
    }
    private void NotifySelection()
    {
        RefreshCommandState(); SelectedModelChanged?.Invoke(SelectedModel); SelectedModelsChanged?.Invoke(SelectedModels);
    }
    public void LoadProject(Project? project, bool notifySelection = true)
    {
        var selected = SelectedModels;
        object? primary = SelectedModel;
        object ExpansionKey(TreeViewItem row) => row.Tag is CollectionSelection folder ? "Folder:" + folder.Kind : row.Tag!;
        var expanded = Rows().Where(row => row.Tag is not null).ToDictionary(ExpansionKey, row => row.IsExpanded);
        _rebuilding = true;
        try
        {
            _project = project; ProjectTreeView.ItemsSource = null;
            if (project is not null)
            {
                var root = Node(Text("TreeProject", "Project"), project);
                root.Items.Add(Node(Text("TreeScene", "Scene"), project.Scene));
                root.Items.Add(Node(Text("TreeSimulation", "Simulation"), project.Simulation));
                root.Items.Add(Node(Text("TreeDisplay", "Display"), project.Display));
                AddCollection(root, "Sources", "TreeSources", project.Sources);
                AddCollection(root, "Parts", "TreeParts", project.Parts);
                AddCollection(root, "Probes", "TreeProbes", project.Probes);
                AddCollection(root, "ColorSchemes", "TreeColorSchemes", project.ColorSchemes);
                ProjectTreeView.ItemsSource = new[] { root };
                foreach (var row in Rows())
                {
                    if (expanded.TryGetValue(ExpansionKey(row), out bool open)) row.IsExpanded = open;
                    if (selected.Any(model => ReferenceEquals(model, row.Tag))) ProjectTreeView.SelectedItems?.Add(row);
                }
                if (primary is not null && selected.Length <= 1) ProjectTreeView.SelectedItem = Rows().FirstOrDefault(row => ReferenceEquals(row.Tag, primary));
            }
        }
        finally { _rebuilding = false; }
        if (notifySelection) NotifySelection(); else RefreshCommandState();
    }
    public void RefreshLanguage() { if (_project is not null) LoadProject(_project, false); }
    private void AddCollection(TreeViewItem parent, string kind, string resource, IEnumerable collection)
    {
        var folder = Node(Text(resource, kind == "ColorSchemes" ? "Colour schemes" : kind), new CollectionSelection(Text(resource, kind), collection, kind));
        foreach (object item in collection) folder.Items.Add(Node(((WaveDudeNext.Model.INamed)item).Name, item));
        parent.Items.Add(folder);
    }
    private TreeViewItem Node(string title, object model)
    {
        var row = new TreeViewItem { Tag = model, IsExpanded = true };
        bool named = model is WaveDudeNext.Model.INamed;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(named ? "Auto,Auto,*,Auto" : "Auto,*,Auto"), MinHeight = 26 };
        var name = new TextBlock { Text = title, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        if (named)
        {
            name.Bind(TextBlock.TextProperty, new Binding("Name") { Source = model });
            name.Bind(ToolTip.TipProperty, new Binding("Name") { Source = model });
        }
        var icon = new IconView { Drawing = Drawing(model), Margin = new Thickness(0, 0, 5, 0) };
        int iconColumn = named ? 1 : 0, nameColumn = named ? 2 : 1, actionsColumn = named ? 3 : 2;
        Grid.SetColumn(icon, iconColumn); Grid.SetColumn(name, nameColumn);
        grid.Children.Add(icon); grid.Children.Add(name);
        string? enabled = model is Probe ? "Active" : model is Source or OpticalPart ? "Enabled" : null;
        if (enabled is not null)
        {
            var check = new CheckBox { Focusable = false, Width = 20, Height = 22, MinHeight = 0, Padding = new Thickness(0), Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            check.Bind(CheckBox.IsCheckedProperty, new Binding(enabled) { Source = model, Mode = BindingMode.TwoWay });
            string tip = model is Probe ? "TipProbeActive" : model is Source ? "TipSourceEnabled" : "TipPartEnabled";
            ToolTip.SetTip(check, Text(tip, enabled)); AutomationProperties.SetName(check, Text(tip, enabled) + ": " + title);
            AutomationProperties.SetAutomationId(check, "Enabled:" + title);
            grid.Children.Add(check);
            icon.Bind(OpacityProperty, new Binding(enabled) { Source = model, Converter = EnabledOpacity });
            name.Bind(OpacityProperty, new Binding(enabled) { Source = model, Converter = EnabledOpacity });
        }
        if (named)
        {
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 0, 0) };
            var list = Collection(model); int index = list?.IndexOf(model) ?? -1;
            actions.Children.Add(ActionButton("Up", "TipMoveUp", "TreeMoveUp:" + title, () => CommandRequested?.Invoke("MoveUp", model), index > 0));
            actions.Children.Add(ActionButton("Down", "TipMoveDown", "TreeMoveDown:" + title, () => CommandRequested?.Invoke("MoveDown", model), list is not null && index >= 0 && index < list.Count - 1));
            actions.Children.Add(ActionButton("Remove", "TipRemove", "TreeRemove:" + title, () => CommandRequested?.Invoke("Remove", model)));
            Grid.SetColumn(actions, actionsColumn); grid.Children.Add(actions);
        }
        else if (model is CollectionSelection folder)
        {
            var add = ActionButton("Add", "TipAdd", "TreeAdd:" + folder.Kind, () => { });
            add.Click += (_, _) => AddToFolder(folder, add);
            Grid.SetColumn(add, actionsColumn); grid.Children.Add(add);
        }
        row.Header = grid;
        AutomationProperties.SetName(row, title); AutomationProperties.SetAutomationId(row, "Tree:" + title);
        return row;
    }
    private static string Drawing(object model) => model switch
    {
        Project => "Project", Scene => "Scene", Simulation => "Simulation", Display => "Display",
        Source => "Source", Probe => "Probe", OpticalPart part => part.GetType().Name.Replace("Part", ""), ColorScheme => "Palette",
        CollectionSelection folder => folder.Kind switch { "Sources" => "Source", "Parts" => "Part", "Probes" => "Probe", _ => "Palette" }, _ => "Project"
    };
    private Button ActionButton(string icon, string tip, string id, Action action, bool enabled = true)
    {
        var button = new Button { Content = new IconView { Drawing = icon }, IsEnabled = enabled, Classes = { "treeAction" } };
        ToolTip.SetTip(button, Text(tip, tip)); AutomationProperties.SetName(button, Text(tip, tip)); AutomationProperties.SetAutomationId(button, id);
        button.Click += (_, _) => action(); return button;
    }
    private IList? Collection(object? model) => model switch { Source => _project?.Sources, OpticalPart => _project?.Parts, Probe => _project?.Probes, ColorScheme => _project?.ColorSchemes, _ => null };
    private void RefreshCommandState()
    {
        if (TreeTools is null) return;
        TreeTools.IsEnabled = _project is not null;
        var list = Collection(SelectedModel); int index = list?.IndexOf(SelectedModel) ?? -1;
        MoveUpButton.IsEnabled = index > 0;
        MoveDownButton.IsEnabled = list is not null && index >= 0 && index < list.Count - 1;
        RemoveButton.IsEnabled = SelectedModels.Any(model => model is WaveDudeNext.Model.INamed);
    }
    private string Text(string key, string fallback) => this.TryFindResource(key, out var value) && value is string label ? label : fallback;
    private void AddToFolder(CollectionSelection folder, Control target)
    {
        if (folder.Kind == "Parts") OpenPartMenu(target);
        else CommandRequested?.Invoke(folder.Kind switch { "Sources" => "AddSource", "Probes" => "AddProbe", _ => "AddScheme" }, null);
    }
    private void OpenPartMenu(Control target)
    {
        var menu = new ContextMenu();
        foreach (var (kind, icon, key) in PartKinds)
        {
            var item = new MenuItem { Header = Text(key, kind.Replace("Part", "")), Icon = new IconView { Drawing = icon }, Tag = kind };
            AutomationProperties.SetAutomationId(item, "TreeAddPart:" + kind);
            ToolTip.SetTip(item, Text("Kind." + kind, kind));
            item.Click += (_, _) => CommandRequested?.Invoke("AddPart", kind);
            menu.Items.Add(item);
        }
        target.ContextMenu = menu; menu.Open(target);
    }
    private static readonly (string Kind, string Icon, string Key)[] PartKinds =
    {
        ("LensPart", "Lens", "MenuAddLens"), ("FiberPart", "Fiber", "MenuAddFiber"), ("BraggReflectorPart", "BraggReflector", "MenuAddBraggReflector"),
        ("GrinLensPart", "GrinLens", "MenuAddGrinLens"), ("ZonePlatePart", "ZonePlate", "MenuAddZonePlate"), ("PolygonPart", "Polygon", "MenuAddPolygon"),
        ("RectanglePart", "Rectangle", "MenuAddRectangle"), ("EllipsePart", "Ellipse", "MenuAddEllipse"), ("TrianglePart", "Triangle", "MenuAddTriangle"), ("OutlinePart", "Outline", "MenuAddOutline"),
        ("XorGatePart", "XorGate", "MenuAddXorGate"), ("AndGatePart", "AndGate", "MenuAddAndGate"), ("OrGatePart", "OrGate", "MenuAddOrGate")
    };
    private void AddSource_Click(object? sender, RoutedEventArgs e) => CommandRequested?.Invoke("AddSource", null);
    private void AddProbe_Click(object? sender, RoutedEventArgs e) => CommandRequested?.Invoke("AddProbe", null);
    private void AddPart_Click(object? sender, RoutedEventArgs e) { if (sender is Control control) OpenPartMenu(control); }
    private void MoveUp_Click(object? sender, RoutedEventArgs e) => CommandRequested?.Invoke("MoveUp", null);
    private void MoveDown_Click(object? sender, RoutedEventArgs e) => CommandRequested?.Invoke("MoveDown", null);
    private void Remove_Click(object? sender, RoutedEventArgs e) => CommandRequested?.Invoke("Remove", null);
    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is Button || (e.Source as Control)?.FindAncestorOfType<Button>() is not null) return;
        if (e.KeyModifiers == KeyModifiers.Alt && e.Key is Key.Up or Key.Down)
        {
            if (e.Key == Key.Up && MoveUpButton.IsEnabled) CommandRequested?.Invoke("MoveUp", null);
            if (e.Key == Key.Down && MoveDownButton.IsEnabled) CommandRequested?.Invoke("MoveDown", null);
            e.Handled = true;
        }
        else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Space)
        {
            foreach (var model in SelectedModels)
                switch (model) { case Source source: source.Enabled = !source.Enabled; break; case OpticalPart part: part.Enabled = !part.Enabled; break; case Probe probe: probe.Active = !probe.Active; break; }
            e.Handled = true;
        }
    }
}

public sealed record CollectionSelection(string Title, IEnumerable Items, string? Kind = null)
{
    public int Count => Items.Cast<object>().Count();
}
