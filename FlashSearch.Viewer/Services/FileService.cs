using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlashSearch.Viewer.Services
{
    public class FileService
    {
        public IEnumerable<LineInfo> GetContextLines(string path, int lineNumber, int contextLines, Regex regex)
        {
            if (lineNumber < 0 || contextLines < 0)
                throw new ArgumentException();
            
            if (!File.Exists(path))
                throw new InvalidOperationException($"Given file {path} does not exist.");

            int begin = lineNumber - contextLines;
            if (begin < 0)
                begin = 0;
            
            int end = lineNumber + contextLines;
            int index = 0;

            
            
            foreach (string line in File.ReadLines(path))
            {
                if (index >= begin)
                {
                    MatchCollection matches = regex.Matches(line);
                    var matchPositions = new List<MatchPosition>(matches.Count);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            matchPositions.Add(new MatchPosition(match.Index, match.Length));
                        }
                    }
                    yield return new LineInfo(line, index, matchPositions);

                }

                ++index;

                if (index > end)
                    yield break;
            }
        }
        
    }

    public class LineInfo
    {
        public string Line { get; }
        public int LineNumber { get; }
        public IEnumerable<MatchPosition> Matches { get; }

        public LineInfo(string line, int lineNumber, IEnumerable<MatchPosition> matches)
        {
            Line = line;
            LineNumber = lineNumber;
            Matches = matches;
        }
    }
}