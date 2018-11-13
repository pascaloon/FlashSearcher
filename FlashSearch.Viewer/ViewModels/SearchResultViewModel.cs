using GalaSoft.MvvmLight;

namespace FlashSearch.Viewer.ViewModels
{
    public class SearchResultViewModel : ViewModelBase
    {
        public SearchResult SearchResult { get; }

        public string File { get; }
        public int LineNumber => SearchResult.LineNumber;
        public string Line => SearchResult.LineContent;

        public SearchResultViewModel(SearchResult searchResult, string rootPath)
        {
            SearchResult = searchResult;
            string shortenedPath = SearchResult.FileInfo.FullName.Replace(rootPath, "");
            shortenedPath = shortenedPath.TrimStart(new char[] {'\\', '/'});
            File = shortenedPath;
        }
        
    }
}