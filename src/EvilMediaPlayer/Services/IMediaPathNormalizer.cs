namespace EvilMediaPlayer.Services;

/// <summary>
///     Service responsible for normalizing media paths for LibVLC.
/// </summary>
public interface IMediaPathNormalizer
{
    /// <summary>
    ///     Normalize a media path to ensure LibVLC can interpret it correctly.
    /// </summary>
    /// <param name="path">Original path</param>
    /// <returns>Normalized path</returns>
    string NormalizePath(string path);
}
