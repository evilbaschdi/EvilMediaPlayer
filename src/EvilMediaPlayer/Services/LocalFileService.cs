using System.Diagnostics;
using Avalonia.Threading;
using EvilMediaPlayer.Models;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Default implementation of local file service.
/// </summary>
public class LocalFileService : ILocalFileService
{
    private readonly IMetadataFetcher _metadataFetcher;

    // Allowed media file extensions
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".mp3", ".m4a", ".flac", ".ogg", ".wma", ".aac", ".wav",
        ".mp4", ".mkv", ".avi", ".mov", ".webm", ".wmv", ".mpg", ".mpeg", ".m4v"
    ];

    /// <summary>
    ///     Initializes a new instance of the LocalFileService.
    /// </summary>
    public LocalFileService(IMetadataFetcher metadataFetcher)
    {
        _metadataFetcher = metadataFetcher ?? throw new ArgumentNullException(nameof(metadataFetcher));
    }

    /// <inheritdoc />
    public async Task EnumerateDirectoryAsync(FileSystemItem parentItem, Action<FileSystemItem> onItemCreated)
    {
        // Perform file system enumeration on a background thread to avoid blocking the UI.
        await Task.Run(() =>
        {
            try
            {
                // Enumerate directories and files on background thread
                var dirInfos = Directory.GetDirectories(parentItem.Path)
                    .Select(dir => new { Name = Path.GetFileName(dir), Path = dir })
                    .ToList();

                var fileInfos = Directory.GetFiles(parentItem.Path)
                    .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Select(file => new { Name = Path.GetFileName(file), Path = file })
                    .ToList();

                // Update UI on UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    var createdFileItems = new List<FileSystemItem>();

                    foreach (var d in dirInfos.OrderBy(x => x.Name))
                    {
                        var item = CreateItem(d.Name, d.Path, true);
                        parentItem.Children.Add(item);
                        onItemCreated?.Invoke(item);
                    }

                    foreach (var f in fileInfos.OrderBy(x => x.Name))
                    {
                        var item = CreateItem(f.Name, f.Path, false);
                        parentItem.Children.Add(item);
                        onItemCreated?.Invoke(item);
                        createdFileItems.Add(item);
                    }

                    // Fetch metadata asynchronously after UI has been updated to keep UI responsive.
                    _ = _metadataFetcher.FetchLocalMetadataAsync(createdFileItems);
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Access denied to {parentItem.Path}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error expanding {parentItem.Path}: {ex.Message}");
            }
        });
    }

    /// <summary>
    ///     Creates a FileSystemItem with lazy-loading placeholder.
    /// </summary>
    private static FileSystemItem CreateItem(string name, string path, bool isDir, bool addDummy = true)
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
            item.Children.Add(null);
        }

        return item;
    }
}
