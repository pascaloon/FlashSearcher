using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlashSearch.Viewer.Services
{
    public class FileService
    {
        private Dictionary<string, List<LineInfo>> _filesCache;

        public FileService()
        {
            _filesCache = new Dictionary<string, List<LineInfo>>();
        }
        
        
        public List<LineInfo> GetMatchesInFile(string path, IContentSelector contentSelector)
        {
            if (_filesCache.ContainsKey(path))
            {
                return _filesCache[path];
            }
            
            if (!File.Exists(path))
                throw new InvalidOperationException($"Given file {path} does not exist.");
            
            var lineInfos = new List<LineInfo>();
            var index = 1;            
            foreach (string line in File.ReadLines(path).ToList())
            {
                lineInfos.Add(new LineInfo(line, index, contentSelector.GetMatches(line).ToList()));
                ++index;
            }

            _filesCache.Add(path, lineInfos);
            return lineInfos;
        }

        public void InvalidateCache()
        {
            _filesCache.Clear();
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