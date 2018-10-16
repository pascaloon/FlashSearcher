using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FlashSearch.Viewer.Converters
{
    public class CustomBooleanToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                throw new ArgumentException("Expected value to be a boolean.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class CustomInvertedBooleanToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isInvisible)
            {
                return isInvisible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
            else
            {
                throw new ArgumentException("Expected value to be a boolean.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}