using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FlashSearch.Viewer.Converters
{
    public class StringContentToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string valueAsString)
            {
                return String.IsNullOrEmpty(valueAsString)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
            else
            {
                throw new ArgumentException("Expected value to be a string.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}