using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlashSearch.Viewer.Services
{
    public class CachedFileContent
    {
        public string Path { get; }
        public List<string> Content { get; }
        private List<LineInfo> _matches;
        
        public CachedFileContent(string path, List<string> content)
        {
            if (content == null) 
                throw new ArgumentNullException(nameof(content));
            Path = path;
            Content = content;
            _matches = Enumerable.Repeat((LineInfo)null, content.Count).ToList();
        }
        
        public CachedFileContent(string path, List<string> content, List<LineInfo> matches)
        {
            if (content == null) 
                throw new ArgumentNullException(nameof(content));
            if (matches == null) 
                throw new ArgumentNullException(nameof(matches));
            if (matches.Count != Content.Count)
                throw new ArgumentException($"{nameof(matches)} length doesn't match {nameof(Content)} length.");
            Path = path;
            Content = content;
            _matches = matches;
        }

        /// <summary>
        /// Get cached searched for line specified.
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <returns>returns NULL if specified line is not cached.</returns>
        public LineInfo GetCachedLinfoForLine(int lineNumber) => _matches[lineNumber];
        public void SetCachedLinfoForLine(int lineNumber, LineInfo lineInfo) => _matches[lineNumber] = lineInfo;

    }
    
    public class FileService
    {
        private ConcurrentDictionary<string, CachedFileContent> _filesContentCache;
        private ConcurrentDictionary<string, long> _filesLastWriteCache;

        public FileService()
        {
            _filesContentCache = new ConcurrentDictionary<string, CachedFileContent>();
            _filesLastWriteCache = new ConcurrentDictionary<string, long>();
        }

        public List<LineInfo> GetMatchesInFilePeek(string path, int lineNumber, int peekSize,
            IContentSelector contentSelector)
        {
            CachedFileContent cache = LoadFile(path);
            return GetMatchesInContent(cache, contentSelector, lineNumber - peekSize, lineNumber + peekSize);
        }
        
        public List<LineInfo> GetMatchesInFile(string path, IContentSelector contentSelector)
        {
            CachedFileContent cache = LoadFile(path);
            return GetMatchesInContent(cache, contentSelector, 1, cache.Content.Count);
        }

        private CachedFileContent LoadFile(string path)
        {
            CachedFileContent cache;
            if (!_filesContentCache.TryGetValue(path, out cache))
            {
                List<string> lines = File.ReadLines(path).ToList();
                cache = new CachedFileContent(path, lines);
                _filesContentCache.AddOrUpdate(path, cache, (s, file) => cache);
            }
            return cache;
        }

        private List<LineInfo> GetMatchesInContent(CachedFileContent fileContent, IContentSelector contentSelector, int lineMin, int lineMax)
        {
            List<LineInfo> lineInfos = new List<LineInfo>();
            var index = 1;
            foreach (string line in fileContent.Content)
            {
                if (index >= lineMin && index <= lineMax)
                {
                    LineInfo lineInfo = fileContent.GetCachedLinfoForLine(index - 1);
                    if (lineInfo == null)
                    {
                        lineInfo = new LineInfo(line, index, contentSelector.GetMatches(line).OrderBy(l => l.Begin).ToList());
                        fileContent.SetCachedLinfoForLine(index - 1, lineInfo);
                    }
                    
                    lineInfos.Add(lineInfo);
                }
                ++index;
            }

            return lineInfos;
        }

        public void InvalidateCache()
        {
            _filesContentCache.Clear();
        }

        public long GetFileLastWriteTimeTicks(FileInfo file)
        {
            long ticks;
            string fileFullName = file.FullName;
            if (!_filesLastWriteCache.TryGetValue(fileFullName, out ticks))
            {
                ticks = file.LastWriteTime.Ticks;
                _filesLastWriteCache.AddOrUpdate(fileFullName, ticks, ((s, l) => ticks));
            }

            return ticks;
        }
    }

    

    public class LineInfo
    {
        public string Content { get; }
        public int LineNumber { get; }
        public IEnumerable<MatchPosition> Matches { get; }

        public LineInfo(string content, int lineNumber, IEnumerable<MatchPosition> matches)
        {
            Content = content;
            LineNumber = lineNumber;
            Matches = matches;
        }
    }
}