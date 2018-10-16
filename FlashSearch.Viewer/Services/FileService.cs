using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlashSearch.Viewer.Services
{
    public class FileService
    {
        public IEnumerable<LineInfo> GetContextLines(string path, int lineNumber, int contextLines, IContentSelector contentSelector)
        {
            if (lineNumber < 0 || contextLines < 0)
                throw new ArgumentException();
            
            if (!File.Exists(path))
                throw new InvalidOperationException($"Given file {path} does not exist.");

            int begin = lineNumber - contextLines;
            if (begin < 1)
                begin = 1;
            
            int end = lineNumber + contextLines;
            int index = 1;

            
            
            foreach (string line in File.ReadLines(path))
            {
                if (index >= begin)
                {
                    yield return new LineInfo(line, index, contentSelector.GetMatches(line).ToList());
                }

                ++index;

                if (index > end)
                    yield break;
            }
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