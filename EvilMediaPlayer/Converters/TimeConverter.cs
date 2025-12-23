using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EvilMediaPlayer.Converters
{
    public class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long ms)
            {
                return TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss");
            }
            return "00:00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FolderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDir)
            {
                return isDir ? FluentAvalonia.UI.Controls.Symbol.Folder : FluentAvalonia.UI.Controls.Symbol.Audio;
            }
            return FluentAvalonia.UI.Controls.Symbol.Help;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayPauseIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
            {
                return isPlaying ? FluentAvalonia.UI.Controls.Symbol.Pause : FluentAvalonia.UI.Controls.Symbol.Play;
            }
            return FluentAvalonia.UI.Controls.Symbol.Play;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
