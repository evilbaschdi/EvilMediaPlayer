using System.Globalization;
using Avalonia.Data.Converters;
using FluentAvalonia.UI.Controls;

namespace EvilMediaPlayer.Converters;

/// <summary>
///     Selects the play or pause icon depending on playback state.
/// </summary>
/// <remarks>
///     Using a converter centralizes the UI's choice of icons for playback state, ensuring all controls
///     render the same visual representation and making future icon changes easier (single change point).
///     A default of `Play` is returned so controls show an actionable state when the source value is missing.
/// </remarks>
public class PlayPauseIconConverter : IValueConverter
{
    /// <summary>
    ///     Converts a boolean playback state to a play or pause icon.
    /// </summary>
    /// <param name="value">A boolean indicating playback state; true for playing, false for paused.</param>
    /// <param name="targetType">The target type of the binding (unused).</param>
    /// <param name="parameter">An optional parameter for converter logic (unused).</param>
    /// <param name="culture">The culture to use in the converter (unused).</param>
    /// <returns>
    ///     Returns <see cref="FASymbol.Pause" /> if playing,
    ///     <see cref="FASymbol.Play" /> otherwise. Defaults to Play if value is not a boolean.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isPlaying)
        {
            return isPlaying ? FASymbol.Pause : FASymbol.Play;
        }

        // Default to Play to indicate the control is ready to start playback when state is unknown.
        return FASymbol.Play;
    }

    /// <summary>
    ///     ConvertBack intentionally not implemented.
    /// </summary>
    /// <remarks>
    ///     Playback state changes should be driven by commands or view-model properties rather than
    ///     mapping UI symbols back to booleans; this avoids brittle parsing logic and keeps responsibilities separated.
    /// </remarks>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}