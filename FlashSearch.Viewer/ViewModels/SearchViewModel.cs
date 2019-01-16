using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        
        private LuceneSearcher _luceneSearcher;
        private LuceneContentSelector _currentContentSelector;

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

        private bool _updateIndex = true;
        public bool UpdateIndex
        {
            get { return _updateIndex; }
            set { Set(ref _updateIndex, value); }
        }
        
        public ObservableCollection<SearchResultViewModel> Results { get; private set; }
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
            FileFilters = new ObservableCollectionRange<FileFilter>(_searchConfig.FileFilters);
            SelectedFileFilter = FileFilters.FirstOrDefault();
            
            Projects = new ObservableCollectionRange<Project>(_searchConfig.Projects);
            SelectedProject = Projects.FirstOrDefault();
            
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
                SelectedFileFilter = FileFilters.FirstOrDefault();
                
                Projects.ResetWith(_searchConfig.Projects);
                SelectedProject = Projects.FirstOrDefault();
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

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private void CancelSearch()
        {
            // Wait 3s max before force kill.
            // TODO: ADD CANCEL SUPPORT FOR LUCENE
//            _luceneSearcher.CancelSearch();
//            Task.Run(() =>
//            {
//                Task.Delay(3000).Wait();
//                if (_searchInProgress)
//                {
//                    _cancellationTokenSource.Cancel();
//                }
//            });
            _luceneSearcher.CancelSearch();
            _cancellationTokenSource.Cancel();
        }

        private void Search()
        {
            _currentContentSelector = new LuceneContentSelector(Query);
            
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
            SearchInProgress = true;
            
            string localDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string indexDirectory = Path.Combine(localDirectory, "Indexes", SelectedProject.Name, SelectedFileFilter.Index);
            _luceneSearcher = new LuceneSearcher(indexDirectory);
            
            SelectedSearchResultViewModel = null;
            Results.Clear();
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                try
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    foreach (SearchResult result in _luceneSearcher.SearchContentInFolder(SelectedProject.Path, fileSelector,
                        _currentContentSelector))
                    {
                        DispatcherHelper.UIDispatcher.Invoke(() =>
                        {
                            Results.Add(new SearchResultViewModel(result, SelectedProject.Path));
                        });
                    }

                    if (_updateIndex)
                    {
                        DispatcherHelper.UIDispatcher.Invoke(() => { IndexingInProgress = true; });
                        
                        _luceneSearcher.IndexContentInFolder(SelectedProject.Path, fileSelector);


                        foreach (SearchResult result in _luceneSearcher.SearchContentInFolder(SelectedProject.Path, fileSelector,
                            _currentContentSelector))
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
                            else if (result.SearchResult.LastIndexTime < result.SearchResult.FileInfo.LastWriteTime.Ticks)
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