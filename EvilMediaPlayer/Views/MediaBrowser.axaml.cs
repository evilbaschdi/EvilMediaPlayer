using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EvilMediaPlayer.Models;
using EvilMediaPlayer.ViewModels;

namespace EvilMediaPlayer.Views
{
    public partial class MediaBrowser : UserControl
    {
        public MediaBrowser()
        {
            InitializeComponent();
        }

        private void OnDoubleTapped(object sender, RoutedEventArgs e)
        {
            if (sender is Control { DataContext: FileSystemItem item })
            {
                if (DataContext is MediaBrowserViewModel vm)
                {
                    vm.ExpandCommand.Execute(item);
                }
            }
        }
    }
}