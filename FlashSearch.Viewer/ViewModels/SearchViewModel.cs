using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using FlashSearch.Configuration;
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
        private SearchConfiguration _searchConfig;
        private readonly ConfigurationWatcher<SearchConfiguration> _searchConfigWatcher;
        
        private FlashSearcher _flashSearcher;
        private RegexContentSelector _currentContentSelector;
        
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

        private string _warning = String.Empty;
        public string Warning
        {
            get => _warning;
            set => Set(ref _warning, value);
        }

        private FileFilter _selectedFileFilter;
        public FileFilter SelectedFileFilter
        {
            get { return _selectedFileFilter; }
            set
            {
                Set(ref _selectedFileFilter, value);
                SelectedFileFilterRegex = _selectedFileFilter?.Regex ?? String.Empty;
            }
        }

        private string _selectedFileFilterRegex;
        public string SelectedFileFilterRegex
        {
            get { return _selectedFileFilterRegex; }
            set { Set(ref _selectedFileFilterRegex, value); }
        }

        private string _selectedFileFilterName;
        public string SelectedFileFilterName
        {
            get { return _selectedFileFilterName; }
            set { Set(ref _selectedFileFilterName, value); }
        }

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
        
        public ObservableCollection<SearchResultViewModel> Results { get; private set; }
        public ObservableCollectionRange<string> RootsHistory { get; private set; }
        public ObservableCollectionRange<FileFilter> FileFilters { get; private set; }
        
        public RelayCommand SearchCommand { get; }
        public RelayCommand CancelSearchCommand { get; }
        public RelayCommand SaveFileFilterCommand { get; }
        
        public SearchViewModel(FileService fileService, ConfigurationWatcher<SearchConfiguration> searchConfigWatcher)
        {
            _fileService = fileService;
            _searchConfigWatcher = searchConfigWatcher;
            _searchConfigWatcher.ConfigurationUpdated += OnConfigurationUpdated;
            _searchConfig = searchConfigWatcher.GetConfiguration();
            _searchInProgress = false;

            Results = new ObservableCollection<SearchResultViewModel>();
            RootsHistory = new ObservableCollectionRange<string>();
            FileFilters = new ObservableCollectionRange<FileFilter>(_searchConfig.FileFilters);
            
            SearchCommand = new RelayCommand(Search, CanSearch);
            CancelSearchCommand = new RelayCommand(CancelSearch, CanCancelSearch);
            SaveFileFilterCommand = new RelayCommand(SaveFileFilter, CanSaveFileFilter);
        }

        private void OnConfigurationUpdated(object sender, SearchConfiguration newConfig)
        {
            _searchConfig = newConfig;
            DispatcherHelper.UIDispatcher.Invoke(() =>
            {
                FileFilters.ResetWith(_searchConfig.FileFilters);
            });
        }

        private bool CanSaveFileFilter()
        {
            return !String.IsNullOrWhiteSpace(_selectedFileFilterName);
        }

        private void SaveFileFilter()
        {
            FileFilter filter = _searchConfig.FileFilters.FirstOrDefault(f => f.Name == _selectedFileFilterName);
            if (filter == null && !String.IsNullOrEmpty(_selectedFileFilterRegex))
            {
                filter = new FileFilter()
                {
                    Name = _selectedFileFilterName, 
                    Regex = _selectedFileFilterRegex
                };
                _searchConfig.FileFilters.Add(filter);
                FileFilters.Add(filter);
            }
            else if (!String.IsNullOrEmpty(_selectedFileFilterRegex))
            {
                filter.Regex = _selectedFileFilterRegex;
            }
            else
            {
                _searchConfig.FileFilters.Remove(filter);
                FileFilters.Remove(filter);
            }
            _searchConfigWatcher.UpdateConfiguration(_searchConfig);
        }

        private bool CanCancelSearch() => SearchInProgress;

        private void CancelSearch()
        {
            // Wait 3s max before force kill.
            _flashSearcher.CancelSearch(3000);
        }

        private void Search()
        {
            RegexContentSelector newContentSelector;
            try
            {
                newContentSelector = new RegexContentSelector(Query);
            }
            catch (Exception)
            {
                Warning = "Content Query: Invalid Regular Expression";
                return;
            }

            IFileSelector fileSelector;
            try
            {
                fileSelector =
                    ExtensibleFileSelectorBuilder.NewFileSelector()
                        .WithExcludedExtensions(_searchConfig.ExcludedExtensions)
                        .WithExcludedPaths(_searchConfig.ExcludedPaths)
                        .WithRegexFilter(SelectedFileFilterRegex)
                        .Build();
            }
            catch (Exception)
            {
                Warning = "Path Query: Invalid Regular Expression";
                return;
            }
            
            Warning = String.Empty;
            _fileService.InvalidateCache();
            _currentContentSelector = newContentSelector;
            SearchInProgress = true;
            _flashSearcher = new FlashSearcher();
            SelectedSearchResultViewModel = null;
            Results.Clear();
            Task.Run(() =>
            {
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