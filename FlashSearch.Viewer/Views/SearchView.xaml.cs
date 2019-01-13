using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FlashSearch.Viewer.Converters;
using FlashSearch.Viewer.ViewModels;

namespace FlashSearch.Viewer.Views
{
    public partial class SearchView : UserControl
    {
        public SearchView()
        {
            InitializeComponent();
        }

        private void DataGrid_OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row;
//            var searchResult = row.DataContext as SearchResultViewModel;
//            row.Foreground = (Brush) new SearchResultStateToBrush().Convert(searchResult.State, typeof(SolidColorBrush), null,
//                CultureInfo.CurrentCulture);
            Binding colorBinding = new Binding("State");
            colorBinding.Converter = new SearchResultStateToBrush();
            
            row.SetBinding(DataGridRow.ForegroundProperty, colorBinding);

        }
    }
}
