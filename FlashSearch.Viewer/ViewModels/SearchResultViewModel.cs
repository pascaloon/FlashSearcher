using GalaSoft.MvvmLight;

namespace FlashSearch.Viewer.ViewModels
{
    public class SearchResultViewModel : ViewModelBase
    {
        public SearchResult SearchResult { get; }

        public string File => SearchResult.FileInfo.FullName;
        public int LineNumber => SearchResult.LineNumber;
        public string Line => SearchResult.LineContent;

        public SearchResultViewModel(SearchResult searchResult)
        {
            SearchResult = searchResult;
        }
        
    }
}