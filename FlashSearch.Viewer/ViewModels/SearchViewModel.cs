using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using FlashSearch.Viewer.Services;
using FlashSearch.Viewer.Utilities;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;

namespace FlashSearch.Viewer.ViewModels
{
    public class SearchEvent
    {
        public SearchResult SelectedResult { get; }
        public IContentSelector ContentSelector { get; }

        public SearchEvent(SearchResult selectedResult, IContentSelector contentSelector)
        {
            SelectedResult = selectedResult;
            ContentSelector = contentSelector;
        }
    }
    
    public class SearchViewModel : ViewModelBase
    {
        private readonly FileService _fileService;
        private readonly SearchConfig _searchConfig;
        private string _rootPath = "D:\\Repository\\FlashSearch";
        public string RootPath
        {
            get => _rootPath;
            set => Set(ref _rootPath, value);
        }
        
        private string _query = "FlashSearch";
        public string Query
        {
            get => _query;
            set => Set(ref _query, value);
        }

        private string _pathQuery;

        public string PathQuery
        {
            get => _pathQuery;
            set => Set(ref _pathQuery, value);
        }
        
        public ObservableCollection<SearchResultViewModel> Results { get; private set; }
        public ObservableCollectionRange<string> RootsHistory { get; private set; }

        
        private RegexContentSelector _currentContentSelector;
        
        private SearchResultViewModel _selectedSearchResultViewModel;

        public SearchResultViewModel SelectedSearchResultViewModel
        {
            get { return _selectedSearchResultViewModel; }
            set
            {
                Set(ref _selectedSearchResultViewModel, value);
                if (_selectedSearchResultViewModel != null)
                    Messenger.Default.Send<SearchEvent>(new SearchEvent(_selectedSearchResultViewModel.SearchResult, _currentContentSelector));

            }
        }

        private FlashSearcher _flashSearcher;

        private bool _searchInProgress;
        public bool SearchInProgress
        {
            get { return _searchInProgress; }
            set
            {
                Set(ref _searchInProgress, value);
                SearchCommand.RaiseCanExecuteChanged();
                CancelSearchCommand.RaiseCanExecuteChanged();
            }
        }
        
        public RelayCommand SearchCommand { get; }
        public RelayCommand CancelSearchCommand { get; }
        
        public SearchViewModel(FileService fileService, SearchConfig searchConfig)
        {
            _fileService = fileService;
            _searchConfig = searchConfig;
            Results = new ObservableCollection<SearchResultViewModel>();
            RootsHistory = new ObservableCollectionRange<string>();
            SearchCommand = new RelayCommand(Search, CanSearch);
            CancelSearchCommand = new RelayCommand(CancelSearch, CanCancelSearch);
            _searchInProgress = false;
        }

        private bool CanCancelSearch() => SearchInProgress;

        private void CancelSearch()
        {
            // Wait 3s max before force kill.
            _flashSearcher.CancelSearch(3000);
        }

        private void Search()
        {
            _fileService.InvalidateCache();
            _currentContentSelector = new RegexContentSelector(Query);
            SearchInProgress = true;
            _flashSearcher = new FlashSearcher();
            SelectedSearchResultViewModel = null;
            Results.Clear();
            Task.Run(() =>
            {
                IFileSelector fileSelector = String.IsNullOrWhiteSpace(PathQuery)
                    ? (IFileSelector) new ExtensionFileSelector(_searchConfig.ExcludedExtensions)
                    : (IFileSelector) new QueryAndExtensionFileSelector(PathQuery, _searchConfig.ExcludedExtensions);
                foreach (SearchResult result in _flashSearcher.SearchContentInFolder(RootPath, fileSelector, _currentContentSelector))
                {
                    DispatcherHelper.UIDispatcher.Invoke(() =>
                    {
                        Results.Add(new SearchResultViewModel(result));
                    });
                }
                
                DispatcherHelper.UIDispatcher.Invoke(() =>
                {
                    SearchInProgress = false;
                });

            });
            
            List<string> usedRoots = RootsHistory.ToList();
            usedRoots.Remove(RootPath);
            usedRoots.Insert(0, RootPath);
            RootsHistory.ResetWith(usedRoots.Take(5));
        }

        private bool CanSearch() => !String.IsNullOrWhiteSpace(RootPath) && !String.IsNullOrWhiteSpace(Query) && !SearchInProgress;
    }
}