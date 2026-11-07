
using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Panes;

public partial class ProjectTree : UserControl
{
    public event Action<object?>? SelectedModelChanged;
    public event Action<object[]>? SelectedModelsChanged;
    public object[] DraggedObjects { get; private set; } = Array.Empty<object>();
    private Project? _project;
    private PointerPressedEventArgs? _dragPress;
    private global::Avalonia.Point _dragOrigin;

    public ProjectTree()
    {
        InitializeComponent();

        ProjectTreeView.AddHandler(PointerPressedEvent, (_, e) =>
        {
            _dragPress = e; _dragOrigin = e.GetPosition(ProjectTreeView);
        }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        ProjectTreeView.PointerMoved += async (_, e) =>
        {
            if (_dragPress is null || !e.GetCurrentPoint(ProjectTreeView).Properties.IsLeftButtonPressed || _project is null) return;
            var pointer = e.GetPosition(ProjectTreeView);
            if (Math.Abs(pointer.X - _dragOrigin.X) + Math.Abs(pointer.Y - _dragOrigin.Y) < 8) return;
            var press = _dragPress; _dragPress = null;
            var models = ProjectTreeView.SelectedItems?.OfType<TreeViewItem>().Select(row => row.Tag).Where(item => item is WaveDudeNext.Model.INamed).Cast<object>().ToArray() ?? Array.Empty<object>();
            if (models.Length == 0) return;
            var data = new DataTransfer(); data.Add(DataTransferItem.CreateText(ProjectClipboard.Write(_project, models)));
            DraggedObjects = models;
            try { await DragDrop.DoDragDropAsync(press, data, DragDropEffects.Copy | DragDropEffects.Move); }
            catch (Exception ex) { Console.WriteLine("Drag failed: " + ex.Message); }
            finally { DraggedObjects = Array.Empty<object>(); }
        };
        ProjectTreeView.SelectionChanged += (_, _) =>
        {
            if (ProjectTreeView.SelectedItem is TreeViewItem row)
                SelectedModelChanged?.Invoke(row.Tag);
            else
                SelectedModelChanged?.Invoke(null);
            SelectedModelsChanged?.Invoke(ProjectTreeView.SelectedItems?.OfType<TreeViewItem>().Select(row => row.Tag).Where(item => item is not null).Cast<object>().ToArray() ?? Array.Empty<object>());
        };
    }

    public object? SelectedModel => (ProjectTreeView.SelectedItem as TreeViewItem)?.Tag;

    public void SelectModel(object? model)
    {
        TreeViewItem? Find(System.Collections.IEnumerable rows)
        {
            foreach (var item in rows)
            {
                if (item is not TreeViewItem row) continue;
                if (ReferenceEquals(row.Tag, model)) return row;
                if (Find(row.Items) is { } child) return child;
            }
            return null;
        }
        ProjectTreeView.SelectedItem = model is null ? null : Find(ProjectTreeView.Items);
    }

    public void LoadProject(Project? project)
    {
        _project = project;
        var selected = SelectedModel;
        ProjectTreeView.ItemsSource = null;

        if (project is null)
            return;

        var root = Node("Project", project);

        root.Items.Add(Node("Scene", project.Scene));
        root.Items.Add(Node(
            "Simulation", project.Simulation));
        root.Items.Add(Node("Display", project.Display));

        AddCollection(root, "Sources", project.Sources);
        AddCollection(root, "Parts", project.Parts);
        AddCollection(root, "Probes", project.Probes);
        AddCollection(root, "Colour schemes",
            project.ColorSchemes);

        ProjectTreeView.ItemsSource =
            new[] { root };
        SelectModel(selected);
    }

    private static void AddCollection(
        TreeViewItem parent,
        string title,
        IEnumerable collection)
    {
        var folder = Node(title, new CollectionSelection(title, collection));

        foreach (var item in collection)
        {
            if (item is null)
                continue;

            string name = item switch
            {
                Source source => source.Name,
                OpticalPart part => part.Name,
                Probe probe => probe.Name,
                _ => item.GetType()
                         .GetProperty("Name")
                         ?.GetValue(item)?.ToString()
                     ?? item.GetType().Name
            };

            folder.Items.Add(Node(name, item));
        }

        parent.Items.Add(folder);
    }

		
	private static TreeViewItem Node(
		string title,
		object model)
	{
		var row = new TreeViewItem
		{
		    Tag = model,
		    IsExpanded = true
		};

		// All optical parts and sources support Enabled.
		if (model is OpticalPart or Source)
		{
		    var checkbox = new CheckBox
		    {
		        Content = title,
		        VerticalAlignment =
		            global::Avalonia.Layout.VerticalAlignment.Center
		    };

		    checkbox.Bind(
		        CheckBox.IsCheckedProperty,
		        new global::Avalonia.Data.Binding("Enabled")
		        {
		            Source = model,
		            Mode = global::Avalonia.Data.BindingMode.TwoWay
		        });

            checkbox.Bind(ContentControl.ContentProperty, new global::Avalonia.Data.Binding("Name") { Source = model });
		    row.Header = checkbox;
		}
		else
		{
            if (model is Probe or WaveDudeNext.Rendering.ColorScheme)
            {
                var name = new TextBlock();
                name.Bind(TextBlock.TextProperty, new global::Avalonia.Data.Binding("Name") { Source = model });
                row.Header = name;
            }
            else row.Header = title;
		}

        string drawing = model switch
        {
            Source => "Source", Probe => "Probe", OpticalPart part => part.GetType().Name.Replace("Part", ""),
            WaveDudeNext.Rendering.ColorScheme => "Palette", Project => "Project", CollectionSelection => "Open", _ => "Project"
        };
        var header = row.Header as Control ?? new TextBlock { Text = title };
        row.Header = null;
        row.Header = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 6,
            Children = { new Controls.IconView { Drawing = drawing }, header } };
		return row;
	}

}


public sealed record CollectionSelection(string Title, System.Collections.IEnumerable Items)
{
    public int Count => Items.Cast<object>().Count();
}
