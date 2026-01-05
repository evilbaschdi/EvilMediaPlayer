using Avalonia.Controls;
using Avalonia.Interactivity;
using EvilMediaPlayer.Models;
using EvilMediaPlayer.ViewModels;

namespace EvilMediaPlayer.Views;

/// <inheritdoc />
public partial class MediaBrowser : UserControl
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public MediaBrowser()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object sender, RoutedEventArgs e)
    {
        // The double-tap handler is UI-specific glue logic: it translates a user gesture into a
        // view-model action. Using DataContext pattern keeps the handler lightweight and makes it easy
        // to unit-test the view model without UI interactions.
        if (sender is not Control { DataContext: FileSystemItem item })
        {
            return;
        }

        if (DataContext is MediaBrowserViewModel vm)
        {
            vm.ExpandCommand.Execute(item);
        }
    }
}