using System.Globalization;
using Avalonia.Data.Converters;

namespace EvilMediaPlayer.Converters;

/// <summary>
///     Converts millisecond durations into a human-readable time string.
/// </summary>
/// <remarks>
///     We keep this converter focused on presentation concerns: it transforms a numeric duration (ms)
///     into a stable, locale-invariant `hh:mm:ss` representation so the UI can display consistent
///     time stamps regardless of user culture settings and avoid surprising formatting differences.
/// </remarks>
public class TimeConverter : IValueConverter
{
    /// <summary>
    ///     Convert milliseconds to a `hh:mm:ss` formatted string.
    /// </summary>
    /// <remarks>
    ///     Returning a well-defined default (`"00:00:00"`) when the input is missing or invalid
    ///     prevents binding errors from propagating to the UI and ensures controls display a safe,
    ///     predictable placeholder instead of failing or showing raw runtime data.
    /// </remarks>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long ms)
        {
            // Use an invariant format to avoid culture-specific separators and keep the UI consistent.
            return TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss");
        }

        // Provide a stable fallback for non-millisecond inputs so callers don't need to guard every binding.
        return "00:00:00";
    }

    /// <summary>
    ///     ConvertBack is intentionally not implemented.
    /// </summary>
    /// <remarks>
    ///     This converter is intended for one-way bindings (model -> view). Implementing ConvertBack would
    ///     introduce parsing logic and potential ambiguity around formats and rounding; leaving it unimplemented
    ///     avoids silent, lossy two-way conversions.
    /// </remarks>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}