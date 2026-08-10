namespace EvilMediaPlayer.Services;

/// <summary>
///     Default implementation of media path normalizer.
/// </summary>
public class MediaPathNormalizer : IMediaPathNormalizer
{
    /// <inheritdoc />
    public string NormalizePath(string path)
    {
        // Remote URLs and special schemes are passed through as-is
        if (path.StartsWith("http") || path.StartsWith("upnp") || path.StartsWith("file"))
        {
            return path;
        }

        // Try to convert local filesystem paths to file:// URIs for consistency
        try
        {
            return new Uri(path).AbsoluteUri;
        }
        catch
        {
            // If URI conversion fails, return the path as-is
            return path;
        }
    }
}
