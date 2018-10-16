using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        private RegexContentSelector _currentContentSelector;
        
        private SearchResultViewModel _selectedSearchResultViewModel;

        public SearchResultViewModel SelectedSearchResultViewModel
        {
            get { return _selectedSearchResultViewModel; }
            set
            {
                Set(ref _selectedSearchResultViewModel, value);
                Messenger.Default.Send<SearchEvent>(new SearchEvent(_selectedSearchResultViewModel.SearchResult, _currentContentSelector));

            }
        }

        private bool _searchInProgress;
        private FlashSearcher _flashSearcher;

        public bool SearchInProgress
        {
            get { return _searchInProgress; }
            set { Set(ref _searchInProgress, value); }
        }
        
        public RelayCommand SearchCommand { get; }
        public RelayCommand CancelSearchCommand { get; }
        
        public SearchViewModel()
        {
            Results = new ObservableCollection<SearchResultViewModel>();
            SearchCommand = new RelayCommand(Search, CanSearch);
            CancelSearchCommand = new RelayCommand(CancelSearch, CanCancelSearch);
            _searchInProgress = false;
        }

        private bool CanCancelSearch() => SearchInProgress;

        private void CancelSearch()
        {
            _flashSearcher.CancelSearch();
        }

        private void Search()
        {
            _currentContentSelector = new RegexContentSelector(Query);
            SearchInProgress = true;
            _flashSearcher = new FlashSearcher();
            Results.Clear();
            Task.Run(() =>
            {
                IFileSelector fileSelector = String.IsNullOrWhiteSpace(PathQuery)
                    ? (IFileSelector) new ExtensionFileSelector()
                    : (IFileSelector) new QueryAndExtensionFileSelector(PathQuery);
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
        }

        private bool CanSearch() => !String.IsNullOrWhiteSpace(RootPath) && !String.IsNullOrWhiteSpace(Query) && !SearchInProgress;
    }
}