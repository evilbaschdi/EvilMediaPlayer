using EvilMediaPlayer.Models;
using LibVLCSharp.Shared;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Service responsible for enumerating and loading DLNA items.
/// </summary>
public interface IDlnaItemService
{
    /// <summary>
    ///     Expand a DLNA device/container and populate its children.
    /// </summary>
    /// <param name="dlnaItem">The DLNA item to expand</param>
    /// <param name="onItemCreated">Callback invoked for each created item to allow wiring up expansion handlers</param>
    /// <returns>A task that completes when expansion is done</returns>
    Task ExpandDlnaItemAsync(FileSystemItem dlnaItem, Action<FileSystemItem> onItemCreated);
}
