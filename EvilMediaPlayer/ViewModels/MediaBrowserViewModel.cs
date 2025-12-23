using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EvilMediaPlayer.Models;
using EvilMediaPlayer.Services;
using LibVLCSharp.Shared;
using Avalonia.Threading;

namespace EvilMediaPlayer.ViewModels
{
    public class MediaBrowserViewModel : ViewModelBase, IDisposable
    {
        private readonly LibVLC _libVlc;
        private readonly DlnaService _dlnaService;

        public ObservableCollection<FileSystemItem> Items { get; }
        public ICommand ExpandCommand { get; }

        public event Action<FileSystemItem> MediaSelected;

        public MediaBrowserViewModel(LibVLC libVlc)
        {
            _libVlc = libVlc ?? throw new ArgumentNullException(nameof(libVlc));
            _dlnaService = new DlnaService(_libVlc);
            _dlnaService.DeviceAdded += OnDlnaDeviceAdded;

            Items = new ObservableCollection<FileSystemItem>();

            // Root for DLNA
            var dlnaRoot = CreateItem("DLNA Servers", null, true, false);
            Items.Add(dlnaRoot);

            // Local Drives
            foreach (var drive in Directory.GetLogicalDrives())
            {
                Items.Add(CreateItem(drive, drive, true));
            }

            ExpandCommand = new RelayCommand(Expand);

            _dlnaService.StartDiscovery();
        }

        public void Dispose()
        {
            _dlnaService.StopDiscovery();
        }

        private FileSystemItem CreateItem(string name, string path, bool isDir, bool addDummy = true)
        {
            var item = new FileSystemItem
            {
                Name = name,
                Path = path,
                IsDirectory = isDir,
                Children = isDir ? new ObservableCollection<FileSystemItem>() : null
            };
            
            if (isDir && addDummy)
            {
                item.Children.Add(null);
            }

            item.OnExpanded += (i) => Expand(i);
            return item;
        }

        private void OnDlnaDeviceAdded(Media media)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var dlnaRoot = Items.FirstOrDefault(i => i.Name == "DLNA Servers");
                if (dlnaRoot != null)
                {
                    // Check if already added
                    if (dlnaRoot.Children.Any(c => c != null && c.Path == media.Mrl))
                        return;

                    dlnaRoot.Children.Add(CreateItem(media.Meta(MetadataType.Title) ?? media.Mrl, media.Mrl, true));
                }
            });
        }

        private async void Expand(object item)
        {
            if (item is FileSystemItem fileSystemItem && fileSystemItem.IsDirectory)
            {
                if (fileSystemItem.Name == "DLNA Servers") return;

                if (fileSystemItem.Children.Count == 1 && fileSystemItem.Children[0] == null)
                {
                    fileSystemItem.Children.Clear();

                    if (fileSystemItem.Path != null && (fileSystemItem.Path.StartsWith("http") || fileSystemItem.Path.StartsWith("upnp")))
                    {
                        await ExpandDlna(fileSystemItem);
                    }
                    else if (fileSystemItem.Path != null)
                    {
                        ExpandLocal(fileSystemItem);
                    }
                }
            }
            else if (item is FileSystemItem fileItem && !fileItem.IsDirectory)
            {
                MediaSelected?.Invoke(fileItem);
            }
        }

        private void ExpandLocal(FileSystemItem fileSystemItem)
        {
            try
            {
                var dirs = Directory.GetDirectories(fileSystemItem.Path)
                    .Select(dir => CreateItem(Path.GetFileName(dir), dir, true));

                var files = Directory.GetFiles(fileSystemItem.Path)
                    .Select(file => CreateItem(Path.GetFileName(file), file, false));

                foreach (var dir in dirs.OrderBy(d => d.Name))
                    fileSystemItem.Children.Add(dir);
                foreach (var file in files.OrderBy(f => f.Name))
                    fileSystemItem.Children.Add(file);
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }
        }

        private async Task ExpandDlna(FileSystemItem dlnaItem)
        {
            System.Diagnostics.Debug.WriteLine($"DLNA: Expanding {dlnaItem.Path}");
            using var media = new Media(_libVlc, dlnaItem.Path, FromType.FromLocation);
            var result = await media.Parse(MediaParseOptions.ParseNetwork);
            
            System.Diagnostics.Debug.WriteLine($"DLNA: Parse result for {dlnaItem.Name}: {result}");

            if (media.SubItems != null)
            {
                System.Diagnostics.Debug.WriteLine($"DLNA: Found {media.SubItems.Count} sub-items");
                foreach (var subItem in media.SubItems)
                {
                    var isDir = subItem.Type == MediaType.Directory || subItem.Type == MediaType.Playlist;
                    dlnaItem.Children.Add(CreateItem(subItem.Meta(MetadataType.Title) ?? subItem.Mrl, subItem.Mrl, isDir));
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DLNA: No sub-items found or sub-items list is null.");
            }
        }
    }
}
