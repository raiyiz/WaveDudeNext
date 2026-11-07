
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WaveDudeNext.Model;
using System.Linq;

namespace WaveDudeNext.Avalonia.Panes;

public partial class PropertiesPane : UserControl
{
    public static readonly StyledProperty<object?>
        SelectedObjectProperty =
        AvaloniaProperty.Register<PropertiesPane, object?>(
            nameof(SelectedObject));

    public static readonly StyledProperty<Project?>
        ProjectProperty =
        AvaloniaProperty.Register<PropertiesPane, Project?>(
            nameof(Project));

    public static readonly StyledProperty<string?> ProjectFolderProperty =
        AvaloniaProperty.Register<PropertiesPane, string?>(nameof(ProjectFolder));

    public string? ProjectFolder
    {
        get => GetValue(ProjectFolderProperty);
        set => SetValue(ProjectFolderProperty, value);
    }

    public PropertiesPane()
    {
        InitializeComponent();
    }

    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public Project? Project
    {
        get => GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }

    public void FocusFirstEditor()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var box = this
                .GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault();

            if (box is not null)
            {
                box.Focus();
                box.SelectAll();
            }
        }, DispatcherPriority.Background);
    }
}

