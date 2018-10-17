using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlashSearch.Viewer.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;

namespace FlashSearch.Viewer.ViewModels
{
    public class ResultPeekViewModel : ViewModelBase
    {
        private readonly FileService _fileService;

        public SearchResult SearchResult { get; }
        public IContentSelector ContentSelector { get; }

        public string FileName => SearchResult.FileInfo.FullName;
        public int LineNumber => SearchResult.LineNumber;
        public int ContextAmount => 3;
        public string LineContent => SearchResult.LineContent;

        private IEnumerable<LineInfo> _fileLines;

        public IEnumerable<LineInfo> FileLines
        {
            get { return _fileLines; }
            set { Set(ref _fileLines, value); }
        }
        
        public ResultPeekViewModel(SearchResult searchResult, IContentSelector contentSelector, FileService fileService)
        {
            _fileService = fileService;
            SearchResult = searchResult;
            ContentSelector = contentSelector;

            Task.Run(() =>
            {
                List<LineInfo> lines = _fileService.GetMatchesInFile(FileName, ContentSelector);
                DispatcherHelper.UIDispatcher.Invoke(() => FileLines = lines);
            });
        }

    }
}