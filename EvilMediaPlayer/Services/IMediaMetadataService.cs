using LibVLCSharp.Shared;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Service responsible for media metadata parsing and track detection.
/// </summary>
public interface IMediaMetadataService
{
    /// <summary>
    ///     Check if the parsed media contains video tracks.
    /// </summary>
    bool HasVideoTracks(Media media);

    /// <summary>
    ///     Get artwork URL from media metadata.
    /// </summary>
    string GetArtworkUrl(Media media);
}
