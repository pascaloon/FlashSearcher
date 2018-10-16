using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FlashSearch.Viewer.Services;
using GalaSoft.MvvmLight;

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

        public ResultPeekViewModel(SearchResult searchResult, IContentSelector contentSelector, FileService fileService)
        {
            _fileService = fileService;
            SearchResult = searchResult;
            ContentSelector = contentSelector;
        }

    }
}