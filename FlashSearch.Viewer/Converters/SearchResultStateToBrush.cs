using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FlashSearch.Viewer.ViewModels;

namespace FlashSearch.Viewer.Converters
{
    public class SearchResultStateToBrush : IValueConverter
    {
        private static Dictionary<SearchResultState, SolidColorBrush> _colors = new Dictionary<SearchResultState, SolidColorBrush>()
        {
            {SearchResultState.InProgress, (SolidColorBrush) Application.Current.MainWindow.FindResource("InProgress")},
            {SearchResultState.Added, (SolidColorBrush) Application.Current.MainWindow.FindResource("Added")},
            {SearchResultState.Removed, (SolidColorBrush) Application.Current.MainWindow.FindResource("Removed")},
            {SearchResultState.Unchanged, (SolidColorBrush) Application.Current.MainWindow.FindResource("Unchanged")}
        };
        
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _colors[(SearchResultState) value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}