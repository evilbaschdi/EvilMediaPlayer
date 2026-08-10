using Avalonia.Media.Imaging;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Service responsible for loading cover artwork from various sources.
/// </summary>
public interface IArtworkService
{
    /// <summary>
    ///     Load cover art from the given URL or file path.
    /// </summary>
    /// <param name="url">URL or file path to the artwork</param>
    /// <returns>Bitmap if successful, null otherwise</returns>
    Task<Bitmap> LoadCoverArtAsync(string url);

    /// <summary>
    ///     Extract cover art from audio file metadata.
    /// </summary>
    /// <param name="filePath">Path to the audio file</param>
    /// <returns>Bitmap if found, null otherwise</returns>
    Task<Bitmap> ExtractCoverArtAsync(string filePath);

    /// <summary>
    ///     Check if the given file is an audio file.
    /// </summary>
    bool IsAudioFile(string filePath);
}
