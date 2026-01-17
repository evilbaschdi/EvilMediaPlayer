using LibVLCSharp.Shared;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Default implementation of media metadata service.
/// </summary>
public class MediaMetadataService : IMediaMetadataService
{
    /// <inheritdoc />
    public bool HasVideoTracks(Media media)
    {
        if (media?.Tracks == null)
        {
            return false;
        }

        return media.Tracks.Any(t => t.TrackType == TrackType.Video);
    }

    /// <inheritdoc />
    public string GetArtworkUrl(Media media)
    {
        if (media == null)
        {
            return null;
        }

        var artworkUrl = media.Meta(MetadataType.ArtworkURL);
        return string.IsNullOrEmpty(artworkUrl) ? null : artworkUrl;
    }
}
