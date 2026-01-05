using Avalonia.Media.Imaging;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Helper for extracting audio metadata (cover art and file-type detection).
/// </summary>
/// <remarks>
///     We isolate TagLib usage here to confine file IO and parsing to a single place and to make
///     it easier to handle native/resource concerns. Bitmap creation happens off the UI thread to
///     avoid blocking rendering while parsing potentially large files.
/// </remarks>
public class AudioMetadataService
{
    /// <summary>
    ///     Extract embedded cover art from an audio file if present.
    /// </summary>
    /// <remarks>
    ///     The extraction is performed on a background thread via Task.Run because TagLib's parsing
    ///     and byte-array allocation can be expensive. Returning a Bitmap keeps the caller API simple
    ///     while ensuring UI thread work is minimal: callers can await this method and assign the result
    ///     directly to view-model properties.
    /// </remarks>
    public async Task<Bitmap> ExtractCoverArtAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        return await Task.Run(() =>
                              {
                                  try
                                  {
                                      // Use TagLibSharp to extract cover art safely
                                      // TagLib.File.Create creates an abstraction and closes the file when disposed.
                                      using var tfile = TagLib.File.Create(filePath);
                                      if (tfile.Tag.Pictures is { Length: > 0 })
                                      {
                                          var pic = tfile.Tag.Pictures[0];
                                          if (pic != null && pic.Data.Data is { Length: > 0 })
                                          {
                                              using var ms = new MemoryStream(pic.Data.Data);
                                              return new Bitmap(ms);
                                          }
                                      }
                                  }
                                  catch (Exception ex)
                                  {
                                      // Failures here are non-fatal for playback; log for diagnostics and return null.
                                      System.Diagnostics.Debug.WriteLine($"Error extracting cover art: {ex.Message}");
                                  }

                                  return null;
                              });
    }

    /// <summary>
    ///     Simple file-extension-based check to determine whether a path likely points to an audio file.
    /// </summary>
    /// <remarks>
    ///     Using a fast extension check avoids opening files unnecessarily. This is a heuristic used to
    ///     decide whether to attempt TagLib-based metadata extraction; it is intentionally conservative.
    /// </remarks>
    public bool IsAudioFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".mp3" or ".m4a" or ".flac" or ".ogg" or ".wma" or ".aac" or ".wav";
    }
}