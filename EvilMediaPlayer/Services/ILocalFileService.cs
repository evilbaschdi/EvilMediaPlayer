using EvilMediaPlayer.Models;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Service responsible for enumerating and loading local file system items.
/// </summary>
public interface ILocalFileService
{
    /// <summary>
    ///     Enumerate local files and directories in the given path.
    /// </summary>
    /// <param name="parentItem">The parent directory item to enumerate</param>
    /// <param name="onItemCreated">Callback invoked for each created item to allow wiring up expansion handlers</param>
    /// <returns>A task that completes when enumeration and metadata loading is done</returns>
    Task EnumerateDirectoryAsync(FileSystemItem parentItem, Action<FileSystemItem> onItemCreated);
}
