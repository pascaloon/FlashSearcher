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
    public class SearchConfiguration
    {
        private readonly IEnumerable<string> _excludedExtensions;
        private readonly IEnumerable<string> _acceptedEncodings;

        public SearchConfiguration(IEnumerable<string> excludedExtensions, IEnumerable<string> acceptedEncodings)
        {
            _acceptedEncodings = acceptedEncodings;
            _excludedExtensions = excludedExtensions.Select(ex => ex.ToLower()).ToList();
        }

        public bool IsExtensionExcluded(string extension)
        {
            return _excludedExtensions.Contains(extension.ToLower());
        }

        public bool IsAcceptedEncoding(Encoding encoding)
        {
            return _acceptedEncodings.Contains(encoding.BodyName);
        }
        
        public static SearchConfiguration Default = new SearchConfiguration(
            new[]{".exe", ".pdb", ".dll", ".db", ".idb", ".obj", ".uasset", ".ipch", ".cache"},
            new[]{"utf-8", "ascii", "iso-8859-1"});
    }
    
    public class MatchPosition
    {
        public int Begin { get; }
        public int Length { get; }

        public MatchPosition(int begin, int length)
        {
            Begin = begin;
            Length = length;
        }
    }
    
    public class SearchResult
    {
        public FileInfo FileInfo { get; }
        public uint LineNumber { get; }
        public string LineContent { get; }
        public Encoding Encoding { get; }
        public IEnumerable<MatchPosition> MatchPositions { get; }

        public SearchResult(FileInfo fileInfo, uint lineNumber, string lineContent, Encoding encoding, IEnumerable<MatchPosition> match)
        {
            FileInfo = fileInfo;
            LineNumber = lineNumber;
            LineContent = lineContent;
            Encoding = encoding;
            MatchPositions = match;
        }
    }
    
    public class FlashSearcher : IEnumerable<SearchResult>, IEnumerator<SearchResult>
    {
        public SearchConfiguration Configuration { get; }

        private bool _completed = false;

        private ConcurrentQueue<SearchResult> _results;
        
        public FlashSearcher(SearchConfiguration configuration)
        {
            Configuration = configuration;
            _completed = false;
            _results = new ConcurrentQueue<SearchResult>();
        }
        
        public IEnumerable<SearchResult> SearchContentInFolder(string directoryPath, string regexQuery)
        {
            var regex = new Regex(regexQuery, RegexOptions.IgnoreCase);
            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            if (!directory.Exists)
            {
                _completed = true;
                return Enumerable.Empty<SearchResult>();
            }
            
            _completed = false;
            Task.Run(() =>
            {
                SearchContentInFolder(directory, regex);
                _completed = true;
            });
            return this;
        }

        private void SearchContentInFolder(DirectoryInfo directory, Regex regex)
        {
            List<Task> tasks = new List<Task>();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            {
                tasks.Add(Task.Run(() => SearchContentInFolder(subDirectory, regex)));
            }

            foreach (FileInfo file in directory.GetFiles())
            {
                if (Configuration.IsExtensionExcluded(file.Extension))
                    continue;

                Encoding encoding = null;
//                Console.WriteLine($"Looking at {file.FullName}");
                using (StreamReader sr = new StreamReader(file.FullName, Encoding.Default, true))
                {
                    if (sr.Peek() >= 0)
                        sr.Read();
                    if (!Configuration.IsAcceptedEncoding(sr.CurrentEncoding))
                    {
//                        Console.WriteLine($"Invalid Encoding ({sr.CurrentEncoding.BodyName}) for {file.FullName}");
                        continue;
                    }

                    encoding = sr.CurrentEncoding;
                }
                
                foreach (SearchResult result in SearchContentInFile(file, regex, encoding))
                {
                    _results.Enqueue(result);
                }
            }

            foreach (Task task in tasks)
            {
                task.Wait();
            }
            
        }

        private IEnumerable<SearchResult> SearchContentInFile(FileInfo file, Regex regex, Encoding encoding)
        {
            uint lineNumber = 0;
            foreach (string line in File.ReadLines(file.FullName, encoding))
            {
                MatchCollection matches = regex.Matches(line);
                if (matches.Count > 0)
                {
                    var matchPositions = new List<MatchPosition>(matches.Count);
                    foreach (Match match in matches)
                    {
                        matchPositions.Add(new MatchPosition(match.Index, match.Length));
                    }
                    
                    yield return new SearchResult(file, lineNumber, line, encoding, matchPositions);

                }
                
                ++lineNumber;
            }
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
            
            while (!_results.TryDequeue(out _current)) { Thread.Sleep(1); }

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