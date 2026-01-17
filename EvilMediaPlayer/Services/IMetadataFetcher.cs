using EvilMediaPlayer.Models;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Service responsible for fetching and applying metadata to media items.
/// </summary>
public interface IMetadataFetcher
{
    /// <summary>
    ///     Fetch metadata for local file system items and apply to the FileSystemItem objects.
    /// </summary>
    /// <param name="items">The items to fetch metadata for</param>
    /// <returns>A task that completes when metadata fetching is done</returns>
    Task FetchLocalMetadataAsync(List<FileSystemItem> items);
}
