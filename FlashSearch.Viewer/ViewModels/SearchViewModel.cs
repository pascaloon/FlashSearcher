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
        
        public RelayCommand SearchCommand { get; }
        
        public SearchViewModel()
        {
            Results = new ObservableCollection<SearchResultViewModel>();
            SearchCommand = new RelayCommand(Search, CanSearch);
        }

        private void Search()
        {
            _currentContentSelector = new RegexContentSelector(Query);
            Results.Clear();
            Task.Run(() =>
            {
                FlashSearcher flashSearcher = new FlashSearcher();
                foreach (SearchResult result in flashSearcher.SearchContentInFolder(RootPath, new AnyFileSelector(), _currentContentSelector))
                {
                    DispatcherHelper.UIDispatcher.Invoke(() =>
                    {
                        Results.Add(new SearchResultViewModel(result));
                    });
                }
            });
        }

        private bool CanSearch() => !String.IsNullOrWhiteSpace(RootPath) && !String.IsNullOrWhiteSpace(Query);
    }
}