using System.Diagnostics;
using Avalonia.Threading;
using EvilMediaPlayer.Models;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Default implementation of metadata fetcher.
/// </summary>
public class MetadataFetcher : IMetadataFetcher
{
    /// <summary>
    ///     Initializes a new instance of the MetadataFetcher.
    /// </summary>
    public MetadataFetcher()
    {
    }

    /// <inheritdoc />
    public async Task FetchLocalMetadataAsync(List<FileSystemItem> items)
    {
        // Batch metadata parsing on a background thread to reduce UI thread work
        await Task.Run(async () =>
        {
            var batch =
                new List<(FileSystemItem Item, string Artist, string Album, string Year, string Duration, uint Track,
                    string Title)>();

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
                    Debug.WriteLine($"Error parsing metadata for {item.Path}: {ex.Message}");
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
                    // Small delay yields better responsiveness on very large folders
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
}
