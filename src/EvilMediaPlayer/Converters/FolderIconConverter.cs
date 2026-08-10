using System.Globalization;
using Avalonia.Data.Converters;
using FluentAvalonia.UI.Controls;

namespace EvilMediaPlayer.Converters;

/// <summary>
///     Chooses an icon symbol based on whether an item is a directory or a file.
/// </summary>
/// <remarks>
///     The UI expects a Symbol value to render consistent visual affordances. Returning a folder
///     or audio symbol aligns user expectations with the underlying model type and improves discoverability.
///     When the input is ambiguous, we return a generic help symbol so the UI indicates uncertainty
///     rather than silently misrepresenting the item type.
/// </remarks>
public class FolderIconConverter : IValueConverter
{
    /// <summary>
    ///     Converts a boolean value indicating whether an item is a directory into a corresponding
    ///     <see cref="FASymbol" /> icon.
    /// </summary>
    /// <param name="value">A boolean indicating if the item is a directory (<c>true</c>) or a file (<c>false</c>).</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional parameter to be used in the converter (not used).</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    ///     <see cref="FASymbol.Folder" /> if <paramref name="value" /> is <c>true</c>,
    ///     <see cref="FASymbol.Audio" /> if <paramref name="value" /> is <c>false</c>,
    ///     or <see cref="FASymbol.Help" /> if <paramref name="value" /> is not a boolean.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isDir)
        {
            return isDir ? FASymbol.Folder : FASymbol.Audio;
        }

        // If we cannot determine the type, present a neutral icon so the UI signals unknown state.
        return FASymbol.Help;
    }

    /// <summary>
    ///     ConvertBack not implemented by design.
    /// </summary>
    /// <remarks>
    ///     Icons are a view concern and should not be mapped back to model booleans; supporting ConvertBack
    ///     would couple presentation decisions to model mutation.
    /// </remarks>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}