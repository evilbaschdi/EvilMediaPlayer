using System.Diagnostics;
using Avalonia.Media.Imaging;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Default implementation of artwork service.
/// </summary>
public class ArtworkService : IArtworkService
{
    private static readonly HttpClient HttpClient = new();
    private readonly AudioMetadataService _audioMetadataService;

    /// <summary>
    ///     Initializes a new instance of the ArtworkService.
    /// </summary>
    public ArtworkService()
    {
        _audioMetadataService = new();
    }

    /// <inheritdoc />
    public async Task<Bitmap> LoadCoverArtAsync(string url)
    {
        try
        {
            // Support different URL schemes; prefer local files where possible to avoid network requests.
            if (url.StartsWith("file://"))
            {
                var localPath = new Uri(url).LocalPath;
                if (File.Exists(localPath))
                {
                    return new(localPath);
                }
            }
            else if (url.StartsWith("http"))
            {
                var data = await HttpClient.GetByteArrayAsync(url);
                using var stream = new MemoryStream(data);
                return new(stream);
            }
            else if (File.Exists(url)) // Plain path
            {
                return new(url);
            }
        }
        catch (Exception ex)
        {
            // Failures to load artwork should not block playback; log for diagnostics and continue.
            Debug.WriteLine($"Failed to load cover art from {url}: {ex.Message}");
        }

        return null;
    }

    /// <inheritdoc />
    public Task<Bitmap> ExtractCoverArtAsync(string filePath) => _audioMetadataService.ExtractCoverArtAsync(filePath);

    /// <inheritdoc />
    public bool IsAudioFile(string filePath) => _audioMetadataService.IsAudioFile(filePath);
}