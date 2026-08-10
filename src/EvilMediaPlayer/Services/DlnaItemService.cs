using System.Diagnostics;
using Avalonia.Threading;
using EvilMediaPlayer.Models;
using LibVLCSharp.Shared;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Default implementation of DLNA item service.
/// </summary>
public class DlnaItemService : IDlnaItemService
{
    private readonly LibVLC _libVlc;

    /// <summary>
    ///     Initializes a new instance of the DlnaItemService.
    /// </summary>
    public DlnaItemService(LibVLC libVlc)
    {
        _libVlc = libVlc ?? throw new ArgumentNullException(nameof(libVlc));
    }

    /// <inheritdoc />
    public async Task ExpandDlnaItemAsync(FileSystemItem dlnaItem, Action<FileSystemItem> onItemCreated)
    {
        Debug.WriteLine($"DLNA: Expanding {dlnaItem.Path}");
        using var media = new Media(_libVlc, dlnaItem.Path, FromType.FromLocation);
        var result = await media.Parse(MediaParseOptions.ParseNetwork);

        Debug.WriteLine($"DLNA: Parse result for {dlnaItem.Name}: {result}");
        Debug.WriteLine($"DLNA: Found {media.SubItems.Count} sub-items");

        // Update UI on the UI thread
        await Dispatcher.UIThread.InvokeAsync(() =>
                                              {
                                                  foreach (var subItem in media.SubItems)
                                                  {
                                                      var isDir = subItem.Type == MediaType.Directory || subItem.Type == MediaType.Playlist;
                                                      var item = CreateItem(subItem.Meta(MetadataType.Title) ?? subItem.Mrl, subItem.Mrl, isDir);
                                                      item.ArtworkUrl = subItem.Meta(MetadataType.ArtworkURL);
                                                      item.Artist = subItem.Meta(MetadataType.Artist);
                                                      item.Album = subItem.Meta(MetadataType.Album);
                                                      item.Year = subItem.Meta(MetadataType.Date)?[..4];
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
                                                      onItemCreated?.Invoke(item);
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
            // Add a null placeholder for lazy-loading
            item.Children.Add(null);
        }

        return item;
    }
}