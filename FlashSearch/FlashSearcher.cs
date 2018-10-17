using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlashSearch
{
    public class SearchResult
    {
        public FileInfo FileInfo { get; }
        public int LineNumber { get; }
        
        public string LineContent { get; }
        public IEnumerable<MatchPosition> MatchPositions { get; }

        public SearchResult(FileInfo fileInfo, int lineNumber, string lineContent, IEnumerable<MatchPosition> match)
        {
            FileInfo = fileInfo;
            LineNumber = lineNumber;
            LineContent = lineContent;
            MatchPositions = match;
        }
    }
    
    public class FlashSearcher : IEnumerable<SearchResult>, IEnumerator<SearchResult>
    {
        private bool _completed = false;

        private ConcurrentQueue<SearchResult> _results;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _forceCancellationTokenSource;
        
        public FlashSearcher()
        {
            _completed = false;
            _results = new ConcurrentQueue<SearchResult>();
        }
        
        public IEnumerable<SearchResult> SearchContentInFolder(string directoryPath, IFileSelector fileSelector, IContentSelector contentSelector)
        {
            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            if (!directory.Exists)
            {
                _completed = true;
                return Enumerable.Empty<SearchResult>();
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            _forceCancellationTokenSource = new CancellationTokenSource();
            _completed = false;
            Task.Run(() =>
            {
                _forceCancellationTokenSource.Token.ThrowIfCancellationRequested();
                SearchContentInFolder(directory, fileSelector, contentSelector);
                _completed = true;
            });
            
            return this;
        }

        private void SearchContentInFolder(DirectoryInfo directory, IFileSelector fileSelector, IContentSelector contentSelector)
        {
            _forceCancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (_cancellationTokenSource.IsCancellationRequested)
                return;
            
            List<Task> tasks = new List<Task>();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            {
                tasks.Add(Task.Run(() => SearchContentInFolder(subDirectory, fileSelector, contentSelector)));
            }

            foreach (FileInfo file in directory.GetFiles())
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    return;
                
                if (!fileSelector.IsFileValid(file))
                    continue;
                
                foreach (SearchResult result in SearchContentInFile(file, contentSelector))
                {
                    _results.Enqueue(result);
                }
            }

            foreach (Task task in tasks)
            {
                task.Wait();
            }
            
        }

        private IEnumerable<SearchResult> SearchContentInFile(FileInfo file, IContentSelector contentSelector)
        {
            int lineNumber = 1;
            foreach (string line in File.ReadLines(file.FullName))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    yield break;
                
                List<MatchPosition> matches = contentSelector.GetMatches(line).ToList();
                
                if (matches.Count > 0)
                {
                    yield return new SearchResult(file, lineNumber, line, matches);
                }
                
                ++lineNumber;
            }
        }

        public void CancelSearch(int maxDelay)
        {
            _cancellationTokenSource.Cancel();
            Task.Run(() =>
            {
                Task.Delay(maxDelay).Wait();
                if (!_completed)
                {
                    _forceCancellationTokenSource.Cancel();
                    _completed = true;
                }
            });
        }
        
        public IEnumerator<SearchResult> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            if (_completed && _results.IsEmpty)
                return false;

            while (!_results.TryDequeue(out _current))
            {
                if (_completed)
                    return false;
                Thread.Sleep(1);
            }

            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        private SearchResult _current = null;
        public SearchResult Current => _current;
        object IEnumerator.Current => Current;
    }
}