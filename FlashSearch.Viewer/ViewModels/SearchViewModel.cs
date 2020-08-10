using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
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
        private readonly ConfigurationPathResolver _configPathResolver;
        
        private LuceneSearcher _luceneSearcher;
        private SmartContentSelector _smartContentSelector;

        private Project _selectedProject;
        public Project SelectedProject
        {
            get { return _selectedProject; }
            set { Set(ref _selectedProject, value); }
        }

        private ObservableCollectionRange<Project> _projects;
        public ObservableCollectionRange<Project> Projects
        {
            get { return _projects; }
            set { Set(ref _projects, value); }
        }
        
        private string _query = "";
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
            }
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
                    Messenger.Default.Send<SearchEvent>(new SearchEvent(_selectedSearchResultViewModel.SearchResult, _smartContentSelector));

            }
        }

        private long _indexedFiles;
        public long IndexedFiles
        {
            get { return _indexedFiles; }
            set { Set(ref _indexedFiles, value); }
        }

        private long _totalFilesFound;
        public long TotalFilesFound
        {
            get { return _totalFilesFound; }
            set { Set(ref _totalFilesFound, value); }
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

        private bool _indexingInProgress;
        public bool IndexingInProgress
        {
            get { return _indexingInProgress; }
            set { Set(ref _indexingInProgress, value); }
        }

        private bool _maxResultsCount;

        public bool MaxResultsCount
        {
            get { return _maxResultsCount; }
            set { Set(ref _maxResultsCount, value); }
        }
        
        private bool _updateIndex = true;
        public bool UpdateIndex
        {
            get { return _updateIndex; }
            set { Set(ref _updateIndex, value); }
        }

        private bool _matchCase = false;
        public bool MatchCase
        {
            get { return _matchCase; }
            set { Set(ref _matchCase, value); }
        }
        
        public ObservableCollection<SearchResultViewModel> Results { get; private set; }
        public ObservableCollectionRange<FileFilter> FileFilters { get; private set; }
        
        public RelayCommand SearchCommand { get; }
        public RelayCommand CancelSearchCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        
        public SearchViewModel(FileService fileService,
            ConfigurationWatcher<SearchConfiguration> searchConfigWatcher,
            ConfigurationPathResolver configPathResolver)
        {
            _fileService = fileService;
            _searchConfigWatcher = searchConfigWatcher;
            _searchConfigWatcher.ConfigurationUpdated += OnConfigurationUpdated;
            _searchConfig = searchConfigWatcher.GetConfiguration();
            _configPathResolver = configPathResolver;
            _searchInProgress = false;

            Results = new ObservableCollection<SearchResultViewModel>();
            FileFilters = new ObservableCollectionRange<FileFilter>(_searchConfig.FileFilters);
            SelectedFileFilter = FileFilters.FirstOrDefault();
            
            Projects = new ObservableCollectionRange<Project>(_searchConfig.Projects);
            SelectedProject = Projects.FirstOrDefault();
            
            SearchCommand = new RelayCommand(Search, CanSearch);
            CancelSearchCommand = new RelayCommand(CancelSearch, CanCancelSearch);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
        }

        private void OpenSettings()
        {
            _fileService.OpenFileInCodeAsync(_searchConfigWatcher.FilePath, 1);
        }

        private void OnConfigurationUpdated(object sender, SearchConfiguration newConfig)
        {
            _searchConfig = newConfig;
            DispatcherHelper.UIDispatcher.Invoke(() =>
            {
                string oldSelectedFileFilter = _selectedFileFilter.Name;
                FileFilters.ResetWith(_searchConfig.FileFilters);
                SelectedFileFilter = FileFilters.FirstOrDefault(ff => ff.Name.Equals(oldSelectedFileFilter)) 
                                     ?? FileFilters.FirstOrDefault();

                string oldSelectedProject = _selectedProject.Name;
                Projects.ResetWith(_searchConfig.Projects);
                SelectedProject = Projects.FirstOrDefault(p => p.Name.Equals(oldSelectedProject)) 
                                  ?? Projects.FirstOrDefault();
            });
        }

        private bool CanSaveFileFilter()
        {
            return !String.IsNullOrWhiteSpace(_selectedFileFilterName);
        }

        private bool CanCancelSearch() => SearchInProgress;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private void CancelSearch()
        {
            _luceneSearcher.CancelSearch();
            _cancellationTokenSource.Cancel();
        }

        private void Search()
        {
            
            IFileSelector fileSelector;
            try
            {
                _smartContentSelector = new SmartContentSelector(Query, MatchCase);
                fileSelector =
                    ExtensibleFileSelectorBuilder.NewFileSelector()
                        .WithExcludedExtensions(_searchConfig.ExcludedExtensions)
                        .WithExcludedPaths(_searchConfig.ExcludedPaths)
                        .WithRegexFilter(SelectedFileFilter.Regex)
                        .WithExclusionRegex(SelectedFileFilter.Exclusion)
                        .WithMaxSize(_searchConfig.MaxFileSize)
                        .Build();
            }
            catch (Exception e)
            {
                Warning = e.Message;
                return;
            }
            
            Warning = String.Empty;
            _fileService.InvalidateCache();
            SearchInProgress = true;
            
            string indexDirectory = _configPathResolver.GetIndexDir(SelectedProject.Name, SelectedFileFilter.Index);
            _luceneSearcher = new LuceneSearcher(indexDirectory);
            
            SelectedSearchResultViewModel = null;
            Results.Clear();
            MaxResultsCount = false;
            _cancellationTokenSource = new CancellationTokenSource();

            Trace.TraceInformation($"[{DateTime.Now}] Searching '{SelectedProject.Path}' in index '{indexDirectory}': '{_smartContentSelector.LuceneQuery}' => '{_smartContentSelector.RegexQuery}'");

            Task.Run(() =>
            {
                try
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    foreach (SearchResult result in _luceneSearcher.SearchContentInFolder(SelectedProject.Path, fileSelector,
                        _smartContentSelector))
                    {
                        DispatcherHelper.UIDispatcher.Invoke(() =>
                        {
                            Results.Add(new SearchResultViewModel(result, SelectedProject.Path));
                            MaxResultsCount = Results.Count >= _luceneSearcher.MaxSearchResults;
                        });
                    }

                    if (_updateIndex)
                    {
                        DispatcherHelper.UIDispatcher.Invoke(() => { IndexingInProgress = true; });
                        
                        _luceneSearcher.IndexContentInFolder(SelectedProject.Path, fileSelector);

                        foreach (SearchResult result in _luceneSearcher.SearchContentInFolder(SelectedProject.Path, fileSelector,
                            _smartContentSelector))
                        {
                            SearchResultViewModel alreadyExisting = Results.FirstOrDefault(r =>
                                r.SearchResult.FileInfo.FullName == result.FileInfo.FullName &&
                                r.SearchResult.LineNumber == result.LineNumber);

                            if (alreadyExisting != null)
                            {
                                alreadyExisting.State = SearchResultState.Unchanged;
                            }
                            else
                            {
                                DispatcherHelper.UIDispatcher.Invoke(() =>
                                {
                                    var newResult = new SearchResultViewModel(result, SelectedProject.Path);
                                    newResult.State = SearchResultState.Added;
                                    Results.Add(newResult);
                                    MaxResultsCount = Results.Count >= _luceneSearcher.MaxSearchResults;
                                });
                            }
                        }

                        foreach (SearchResultViewModel result in Results)
                        {

                            if (result.State == SearchResultState.InProgress)
                            {
                                result.State = SearchResultState.Removed;
                            }
                        }
                        
                        DispatcherHelper.UIDispatcher.Invoke(() => { IndexingInProgress = false; });
                    }
                    else
                    {
                        foreach (SearchResultViewModel result in Results)
                        {
                            if (!result.SearchResult.FileInfo.Exists)
                            {
                                result.State = SearchResultState.Removed;
                            }
                            else if (result.SearchResult.LastIndexTime < _fileService.GetFileLastWriteTimeTicks(result.SearchResult.FileInfo))
                            {
                                result.State = SearchResultState.InProgress;
                            }
                            else
                            {
                                result.State = SearchResultState.Unchanged;
                            }
                        }
                    }
                    
                    DispatcherHelper.UIDispatcher.Invoke(() =>
                    {
                        if (Results.Count == 0)
                        {
                            Warning = "No Results Found!";
                        }
                    });
                }
                catch (Exception e)
                {
                    DispatcherHelper.UIDispatcher.Invoke(() =>
                    {
                        SearchInProgress = false;
                        Warning = e.Message;
                    });
                }
                finally
                {
                    DispatcherHelper.UIDispatcher.Invoke(() => {
                        SearchInProgress = false;
                        IndexingInProgress = false;
                    });

                }
            });

            Task.Run(() =>
            {
                while (SearchInProgress)
                {
                    IndexedFiles = _luceneSearcher.FilesIndexed;
                    TotalFilesFound = _luceneSearcher.TotalFilesFound;
                    Thread.Sleep(100);
                }
            });

        }

        private bool CanSearch() => SelectedProject != null && !String.IsNullOrWhiteSpace(Query) && !SearchInProgress;
    }
}