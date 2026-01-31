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
/// </summary>
/// <remarks>
///     Delegates file enumeration, metadata extraction, and DLNA handling to specialized services.
///     This keeps concerns separated per SOLID principles (Single Responsibility, Dependency Inversion).
/// </remarks>
public class MediaBrowserViewModel : ViewModelBase, IDisposable
{
    private readonly IDlnaService _dlnaService;
    private readonly ILocalFileService _localFileService;
    private readonly IDlnaItemService _dlnaItemService;

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
    /// <param name="dlnaService">Service for DLNA device discovery</param>
    /// <param name="localFileService">Service for local file enumeration</param>
    /// <param name="dlnaItemService">Service for DLNA item expansion</param>
    /// <exception cref="ArgumentNullException">Thrown if any service is null.</exception>
    public MediaBrowserViewModel(
        IDlnaService dlnaService,
        ILocalFileService localFileService,
        IDlnaItemService dlnaItemService)
    {
        _dlnaService = dlnaService ?? throw new ArgumentNullException(nameof(dlnaService));
        _localFileService = localFileService ?? throw new ArgumentNullException(nameof(localFileService));
        _dlnaItemService = dlnaItemService ?? throw new ArgumentNullException(nameof(dlnaItemService));

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

                if (fileSystemItem.Path != null &&
                    (fileSystemItem.Path.StartsWith("http") || fileSystemItem.Path.StartsWith("upnp")))
                {
                    await _dlnaItemService.ExpandDlnaItemAsync(fileSystemItem, OnItemCreated);
                }
                else if (fileSystemItem.Path != null)
                {
                    await _localFileService.EnumerateDirectoryAsync(fileSystemItem, OnItemCreated);
                }
            }
        }
        else if (item is FileSystemItem { IsDirectory: false } fileItem)
        {
            MediaSelected?.Invoke(fileItem);
        }
    }

    /// <summary>
    ///     Called when a new item is created to wire up expansion handlers.
    /// </summary>
    private void OnItemCreated(FileSystemItem item)
    {
        item.OnExpanded += async fsItem => await Expand(fsItem);
    }
}