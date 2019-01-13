using GalaSoft.MvvmLight;

namespace FlashSearch.Viewer.ViewModels
{
    public enum SearchResultState
    {
        Unchanged,
        Added,
        Removed,
        InProgress
    }
    
    public class SearchResultViewModel : ViewModelBase
    {
        public SearchResult SearchResult { get; }

        public string File { get; }
        public int LineNumber => SearchResult.LineNumber;
        public string Line => SearchResult.LineContent;
        
        private SearchResultState _state;
        public SearchResultState State
        {
            get { return _state; }
            set { Set(ref _state, value); }
        }

        public SearchResultViewModel(SearchResult searchResult, string rootPath)
        {
            SearchResult = searchResult;
            string shortenedPath = SearchResult.FileInfo.FullName.Replace(rootPath, "");
            shortenedPath = shortenedPath.TrimStart(new char[] {'\\', '/'});
            File = shortenedPath;
            State = SearchResultState.InProgress;
        }
        
    }
}