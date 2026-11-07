
using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls;
using WaveDudeNext.Model;

namespace WaveDudeNext.Avalonia.Panes;

public partial class ProjectTree : UserControl
{
    public event Action<object?>? SelectedModelChanged;

    public ProjectTree()
    {
        InitializeComponent();

        ProjectTreeView.SelectionChanged += (_, _) =>
        {
            if (ProjectTreeView.SelectedItem is TreeViewItem row)
                SelectedModelChanged?.Invoke(row.Tag);
            else
                SelectedModelChanged?.Invoke(null);
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

		return row;
	}

}


public sealed record CollectionSelection(string Title, System.Collections.IEnumerable Items)
{
    public int Count => Items.Cast<object>().Count();
}
