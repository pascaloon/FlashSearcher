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
        public Regex UsedRegex { get; }

        public SearchEvent(SearchResult selectedResult, Regex usedRegex)
        {
            SelectedResult = selectedResult;
            UsedRegex = usedRegex;
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

        private SearchResultViewModel _selectedSearchResultViewModel;

        public SearchResultViewModel SelectedSearchResultViewModel
        {
            get { return _selectedSearchResultViewModel; }
            set
            {
                Set(ref _selectedSearchResultViewModel, value);
                Messenger.Default.Send<SearchEvent>(new SearchEvent(_selectedSearchResultViewModel.SearchResult, new Regex(Query, RegexOptions.IgnoreCase)));

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
            Results.Clear();
            Task.Run(() =>
            {
                FlashSearcher flashSearcher = new FlashSearcher(SearchConfiguration.Default);
                foreach (SearchResult result in flashSearcher.SearchContentInFolder(RootPath, Query))
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