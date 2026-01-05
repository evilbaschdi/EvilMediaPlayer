using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Threading;
using EvilMediaPlayer.Models;
using EvilMediaPlayer.Services;
using LibVLCSharp.Shared;
using ReactiveUI;

namespace EvilMediaPlayer.ViewModels;

/// <summary>
///     ViewModel for browsing local file system and DLNA media servers.
///     Provides an observable collection of <see cref="FileSystemItem" /> for UI binding,
///     supports lazy expansion of directories and DLNA devices, and exposes commands for expanding items.
/// </summary>
/// <remarks>
///     This view-model bridges file system and DLNA enumeration with UI binding. It starts DLNA discovery
///     on creation and manages lazy-loading of directories and DLNA servers to keep the UI responsive.
///     Media selection is exposed via the MediaSelected event so playback can be decoupled from browsing.
/// </remarks>
public class MediaBrowserViewModel : ViewModelBase, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly DlnaService _dlnaService;

    /// <summary>
    ///     The observable collection of root-level items (DLNA root + local drives).
    /// </summary>
    /// <remarks>
    ///     Exposed so the UI can bind a tree control directly. Items are populated on initialization and
    ///     grow dynamically as the user expands directories or DLNA devices. The collection itself never
    ///     changes; instead, child items are added to expanded items' Children collection.
    /// </remarks>
    public ObservableCollection<FileSystemItem> Items { get; }

    /// <summary>
    ///     Command to expand a directory or DLNA item and populate its children.
    /// </summary>
    /// <remarks>
    ///     Exposed as a ReactiveCommand so the UI can invoke it when a tree node is expanded. The command
    ///     routes to the Expand method which determines whether to load local or DLNA children based on the item's path.
    /// </remarks>
    public ReactiveCommand<object, Unit> ExpandCommand { get; }

    /// <summary>
    ///     Event raised when the user selects a media file (not a directory).
    /// </summary>
    /// <remarks>
    ///     Consumers (typically MainWindowViewModel) listen to this event to start playback of the selected
    ///     file. Using an event decouples media browsing from playback concerns.
    /// </remarks>
    public event Action<FileSystemItem> MediaSelected;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="libVlc">The LibVLC instance used for DLNA discovery and parsing.</param>
    /// <exception cref="ArgumentNullException">Thrown if libVlc is null.</exception>
    /// <remarks>
    ///     Initializes the browser with root items (DLNA root and local drives) and starts DLNA device
    ///     discovery in the background. DLNA devices found during discovery are added dynamically to the
    ///     DLNA root as they become available.
    /// </remarks>
    public MediaBrowserViewModel(LibVLC libVlc)
    {
        _libVlc = libVlc ?? throw new ArgumentNullException(nameof(libVlc));
        _dlnaService = new DlnaService(_libVlc);
        _dlnaService.DeviceAdded += OnDlnaDeviceAdded;

        Items = [];

        // Root for DLNA
        var dlnaRoot = CreateItem("DLNA Servers", null, true, false);
        Items.Add(dlnaRoot);

        // Local Drives
        foreach (var drive in Directory.GetLogicalDrives())
        {
            Items.Add(CreateItem(drive, drive, true));
        }

        ExpandCommand = ReactiveCommand.CreateFromTask<object>(Expand);

        _dlnaService.StartDiscovery();
    }

    /// <summary>
    ///     Stops DLNA discovery and releases resources.
    /// </summary>
    /// <remarks>
    ///     Called when the view-model is disposed to cleanly shut down DLNA device discovery and free
    ///     native resources held by the DlnaService.
    /// </remarks>
    public void Dispose()
    {
        _dlnaService.StopDiscovery();
    }

    /// <summary>
    ///     Creates a FileSystemItem with optional lazy-loading placeholder.
    /// </summary>
    /// <remarks>
    ///     This factory method centralizes item creation logic and ensures consistent initialization.
    ///     For directories, it adds a null placeholder child (when addDummy is true) so the UI displays
    ///     an expand glyph without immediately loading children. The OnExpanded callback is wired
    ///     so the view-model can handle expansion asynchronously.
    /// </remarks>
    /// <param name="name">The display name of the item.</param>
    /// <param name="path">The file system or DLNA path (null for DLNA root).</param>
    /// <param name="isDir">True if the item is a directory or DLNA container.</param>
    /// <param name="addDummy">True to add a null placeholder for lazy-loading (default true).</param>
    /// <returns>A configured FileSystemItem.</returns>
    private FileSystemItem CreateItem(string name, string path, bool isDir, bool addDummy = true)
    {
        var item = new FileSystemItem
                   {
                       Name = name,
                       Path = path,
                       IsDirectory = isDir,
                       Children = isDir ? [] : null
                   };

        if (isDir && addDummy)
        {
            // Add a null placeholder so the UI shows an expand glyph without loading contents immediately.
            // This pattern allows lazy-loading directories on demand which avoids expensive IO on startup.
            item.Children.Add(null);
        }

        item.OnExpanded += async fsItem => await Expand(fsItem);
        return item;
    }

    /// <summary>
    ///     Handles DLNA device discovery events and adds new devices to the UI.
    /// </summary>
    /// <remarks>
    ///     Called asynchronously when the DlnaService discovers a new device. This method runs on a
    ///     background thread, so all UI updates are dispatched to the UI thread via Dispatcher.UIThread.
    ///     Devices are added only if not already present (checked by path) to avoid duplicates.
    /// </remarks>
    /// <param name="media">The discovered DLNA device/media from LibVLC.</param>
    private void OnDlnaDeviceAdded(Media media)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
                                        {
                                            var dlnaRoot = Items.FirstOrDefault(i => i.Name == "DLNA Servers");
                                            if (dlnaRoot != null)
                                            {
                                                // Check if already added
                                                if (dlnaRoot.Children.Any(c => c != null && c.Path == media.Mrl))
                                                {
                                                    return;
                                                }

                                                // Add device under the DLNA root. We prefer metadata title but fall back to MRL to remain robust.
                                                dlnaRoot.Children.Add(CreateItem(media.Meta(MetadataType.Title) ?? media.Mrl, media.Mrl, true));
                                            }
                                        });
    }

    /// <summary>
    ///     Handles expansion of tree items: either loads children or fires MediaSelected for files.
    /// </summary>
    /// <remarks>
    ///     This method is invoked by the ExpandCommand when the UI expands a node. For directories,
    ///     it replaces the null placeholder with actual children (either local files or DLNA items).
    ///     For non-directories, it raises the MediaSelected event so the parent view-model can start playback.
    /// </remarks>
    /// <param name="item">The item being expanded (or selected if it's a file).</param>
    private async Task Expand(object item)
    {
        if (item is FileSystemItem { IsDirectory: true } fileSystemItem)
        {
            if (fileSystemItem.Name == "DLNA Servers")
            {
                return;
            }

            if (fileSystemItem.Children.Count == 1 && fileSystemItem.Children[0] == null)
            {
                fileSystemItem.Children.Clear();

                if (fileSystemItem.Path != null && (fileSystemItem.Path.StartsWith("http") || fileSystemItem.Path.StartsWith("upnp")))
                {
                    await ExpandDlna(fileSystemItem);
                }
                else if (fileSystemItem.Path != null)
                {
                    await ExpandLocal(fileSystemItem);
                }
            }
        }
        else if (item is FileSystemItem { IsDirectory: false } fileItem)
        {
            MediaSelected?.Invoke(fileItem);
        }
    }

    // Allowed media file extensions. Kept as a HashSet for fast lookup during directory scans.
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".mp3", ".m4a", ".flac", ".ogg", ".wma", ".aac", ".wav",
        ".mp4", ".mkv", ".avi", ".mov", ".webm", ".wmv", ".mpg", ".mpeg", ".m4v"
    ];

    /// <summary>
    ///     Enumerates local file system directory and populates the item's children.
    /// </summary>
    /// <remarks>
    ///     This method runs on a background thread to avoid blocking the UI during file enumeration.
    ///     Directories and filtered media files are added to the UI immediately (sorted alphabetically),
    ///     then metadata is fetched asynchronously in batches to keep the UI responsive even when
    ///     handling large directories with many files.
    /// </remarks>
    /// <param name="fileSystemItem">The directory item to expand.</param>
    private async Task ExpandLocal(FileSystemItem fileSystemItem)
    {
        // Perform file system enumeration on a background thread to avoid blocking the UI.
        await Task.Run(() =>
                       {
                           try
                           {
                               // Enumerate directories and files on background thread
                               var dirInfos = Directory.GetDirectories(fileSystemItem.Path)
                                                       .Select(dir => new { Name = Path.GetFileName(dir), Path = dir })
                                                       .ToList();

                               var fileInfos = Directory.GetFiles(fileSystemItem.Path)
                                                        .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                                                        .Select(file => new { Name = Path.GetFileName(file), Path = file })
                                                        .ToList();

                               // Update UI on UI thread
                               Dispatcher.UIThread.Post(() =>
                                                        {
                                                            var createdFileItems = new List<FileSystemItem>();

                                                            foreach (var d in dirInfos.OrderBy(x => x.Name))
                                                            {
                                                                fileSystemItem.Children.Add(CreateItem(d.Name, d.Path, true));
                                                            }

                                                            foreach (var f in fileInfos.OrderBy(x => x.Name))
                                                            {
                                                                var item = CreateItem(f.Name, f.Path, false);
                                                                fileSystemItem.Children.Add(item);
                                                                createdFileItems.Add(item);
                                                            }

                                                            // Fetch metadata asynchronously after UI has been updated to keep UI responsive.
                                                            _ = FetchLocalMetadata(createdFileItems);
                                                        });
                           }
                           catch (UnauthorizedAccessException ex)
                           {
                               System.Diagnostics.Debug.WriteLine($"Access denied to {fileSystemItem.Path}: {ex.Message}");
                           }
                           catch (Exception ex)
                           {
                               System.Diagnostics.Debug.WriteLine($"Error expanding {fileSystemItem.Path}: {ex.Message}");
                           }
                       });
    }

    /// <summary>
    ///     Extracts and applies metadata (artist, album, duration, etc.) to media file items.
    /// </summary>
    /// <remarks>
    ///     This method parses files using TagLib in batches on a background thread. Updates are
    ///     posted to the UI thread in chunks (every 20 files or at the end) to avoid overwhelming
    ///     the UI with rapid property changes. The batching strategy and small delays yield better
    ///     overall responsiveness when processing large numbers of files.
    /// </remarks>
    /// <param name="items">The list of file items to fetch metadata for.</param>
    private async Task FetchLocalMetadata(List<FileSystemItem> items)
    {
        // Batch metadata parsing on a background thread to reduce UI thread work and avoid long pauses when many files exist.
        await Task.Run(async () =>
                       {
                           var batch = new List<(FileSystemItem Item, string Artist, string Album, string Year, string Duration, uint Track, string Title)>();

                           foreach (var item in items.Where(item => !item.IsDirectory))
                           {
                               try
                               {
                                   using var file = TagLib.File.Create(item.Path);
                                   var tag = file.Tag;
                                   var props = file.Properties;

                                   var artist = tag.FirstPerformer ?? tag.FirstAlbumArtist;
                                   var album = tag.Album;
                                   var year = tag.Year > 0 ? tag.Year.ToString() : null;
                                   var duration = props.Duration;
                                   var durText = duration.TotalSeconds > 0 ? duration.ToString(@"mm\:ss") : null;
                                   var track = tag.Track;
                                   var title = tag.Title;

                                   batch.Add((item, artist, album, year, durText, track, title));
                               }
                               catch (Exception ex)
                               {
                                   System.Diagnostics.Debug.WriteLine($"Error parsing metadata for {item.Path}: {ex.Message}");
                               }

                               if (batch.Count >= 20)
                               {
                                   var currentBatch = batch.ToList();
                                   batch.Clear();
                                   Dispatcher.UIThread.Post(() =>
                                                            {
                                                                foreach (var data in currentBatch)
                                                                {
                                                                    data.Item.Artist = data.Artist;
                                                                    data.Item.Album = data.Album;
                                                                    data.Item.Year = data.Year;
                                                                    data.Item.TrackNumber = data.Track.ToString("#'.'");
                                                                    data.Item.Title = data.Title;
                                                                    if (data.Duration != null)
                                                                    {
                                                                        data.Item.DurationText = data.Duration;
                                                                    }
                                                                }
                                                            });
                                   // Small delay yields better responsiveness on very large folders.
                                   await Task.Delay(10);
                               }
                           }

                           if (batch.Count > 0)
                           {
                               var currentBatch = batch.ToList();
                               Dispatcher.UIThread.Post(() =>
                                                        {
                                                            foreach (var data in currentBatch)
                                                            {
                                                                data.Item.Artist = data.Artist;
                                                                data.Item.Album = data.Album;
                                                                data.Item.Year = data.Year;
                                                                data.Item.TrackNumber = data.Track.ToString("#'.'");
                                                                data.Item.Title = data.Title;
                                                                if (data.Duration != null)
                                                                {
                                                                    data.Item.DurationText = data.Duration;
                                                                }
                                                            }
                                                        });
                           }
                       });
    }

    /// <summary>
    ///     Expands a DLNA device node and populates it with available media and sub-containers.
    /// </summary>
    /// <remarks>
    ///     This method queries the DLNA server via LibVLC's Media parsing, extracts metadata from
    ///     sub-items (title, artwork, artist, album, track number), and creates FileSystemItem
    ///     objects for each sub-item. Directory-type items are marked as expandable for further
    ///     browsing of nested containers.
    /// </remarks>
    /// <param name="dlnaItem">The DLNA device/container item to expand.</param>
    private async Task ExpandDlna(FileSystemItem dlnaItem)
    {
        System.Diagnostics.Debug.WriteLine($"DLNA: Expanding {dlnaItem.Path}");
        using var media = new Media(_libVlc, dlnaItem.Path, FromType.FromLocation);
        var result = await media.Parse(MediaParseOptions.ParseNetwork);

        System.Diagnostics.Debug.WriteLine($"DLNA: Parse result for {dlnaItem.Name}: {result}");

        System.Diagnostics.Debug.WriteLine($"DLNA: Found {media.SubItems.Count} sub-items");
        foreach (var subItem in media.SubItems)
        {
            var isDir = subItem.Type == MediaType.Directory || subItem.Type == MediaType.Playlist;
            var item = CreateItem(subItem.Meta(MetadataType.Title) ?? subItem.Mrl, subItem.Mrl, isDir);
            item.ArtworkUrl = subItem.Meta(MetadataType.ArtworkURL);
            item.Artist = subItem.Meta(MetadataType.Artist);
            item.Album = subItem.Meta(MetadataType.Album);
            item.Year = subItem.Meta(MetadataType.Date)?.Substring(0, 4);
            item.Title = subItem.Meta(MetadataType.Title);
            if (uint.TryParse(subItem.Meta(MetadataType.TrackNumber), out var trackNum) && !isDir && trackNum > 0)
            {
                item.TrackNumber = trackNum.ToString("#'.'");
            }

            if (subItem.Duration > 0)
            {
                item.DurationText = TimeSpan.FromMilliseconds(subItem.Duration).ToString(@"mm\:ss");
            }

            dlnaItem.Children.Add(item);
        }
    }
}