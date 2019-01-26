using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FlashSearch.Viewer.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
        private CancellationTokenSource _cancellationTokenSource;

        private IEnumerable<LineInfo> _fileLines;
        public IEnumerable<LineInfo> FileLines
        {
            get { return _fileLines; }
            set { Set(ref _fileLines, value); }
        }

        private bool _showFileCopiedPopup;

        public bool ShowFileCopiedPopup
        {
            get { return _showFileCopiedPopup; }
            set { Set(ref _showFileCopiedPopup, value); }
        }
        
        public RelayCommand OpenFileInCodeCommand { get; }
        public RelayCommand OpenFileCommand { get; }
        public RelayCommand CopyFilePathCommand { get; }
        private bool _fileOpened;

        public bool FileOpened
        {
            get { return _fileOpened; }
            set
            {
                Set(ref _fileOpened, value);
                OpenFileCommand.RaiseCanExecuteChanged();
            }
        }
        
        public ResultPeekViewModel(SearchResult searchResult, IContentSelector contentSelector, FileService fileService)
        {
            _fileService = fileService;
            SearchResult = searchResult;
            ContentSelector = contentSelector;
            OpenFileCommand = new RelayCommand(OpenFile, () => !FileOpened);
            OpenFileInCodeCommand = new RelayCommand(OpenFileInCode);
            CopyFilePathCommand = new RelayCommand(CopyFilePath);
            
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                List<LineInfo> lines = _fileService.GetMatchesInFilePeek(FileName, SearchResult.LineNumber, 10, ContentSelector);
                DispatcherHelper.UIDispatcher.Invoke(() => FileLines = lines);
            }, _cancellationTokenSource.Token);
        }

        private void CopyFilePath()
        {
            Clipboard.SetText(SearchResult.FileInfo.FullName);
            ShowFileCopiedPopup = true;

            Task.Delay(TimeSpan.FromSeconds(2))
                .ContinueWith((t) => DispatcherHelper.UIDispatcher.Invoke(
                    () => ShowFileCopiedPopup = false));
        }

        private void OpenFileInCode()
        {
            _fileService.OpenFileInCodeAsync(FileName, LineNumber);
        }

        private void OpenFile()
        {
            Task.Run(() =>
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                List<LineInfo> lines = _fileService.GetMatchesInFile(FileName, ContentSelector);
                DispatcherHelper.UIDispatcher.Invoke(() =>
                {
                    FileLines = lines;
                    FileOpened = true;
                });
            }, _cancellationTokenSource.Token);
        }

        public override void Cleanup()
        {
            _cancellationTokenSource.Cancel();
            base.Cleanup();
        }
    }
}