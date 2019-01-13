using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlashSearch
{
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
    
    public interface IContentSelector
    {
        IEnumerable<MatchPosition> GetMatches(string line);
    }
    
    public class RegexContentSelector : IContentSelector
    {
        private readonly Regex _regex;
        
        public RegexContentSelector(string regex)
        {
            _regex = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        
        public IEnumerable<MatchPosition> GetMatches(string line)
        {
            MatchCollection matches = _regex.Matches(line);
            foreach (Match match in matches)
            {
                yield return new MatchPosition(match.Index, match.Length);
            }
        }
    }
    
    public class LuceneContentSelector: IContentSelector
    {
        public string LuceneQuery { get; }

        private readonly List<Regex> _regexes;

        
        public LuceneContentSelector(string luceneQuery)
        {
            LuceneQuery = luceneQuery;
            _regexes = new List<Regex>();
            foreach (string word in luceneQuery.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries))
            {
                string regexString = word
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("+", "")
                    .Replace("\\", "")
                    .Replace(".", "")
                    .Replace("\"", "")
                    .Replace("AND", "")
                    .Replace("OR", "")
                    .Replace("\"", "")
                    .Replace("*", "\\w*");
                if (String.IsNullOrWhiteSpace(regexString))
                    continue;
                _regexes.Add(new Regex(regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled));
            }
        }
        
        public IEnumerable<MatchPosition> GetMatches(string line)
        {
            foreach (Regex regex in _regexes)
            {
                MatchCollection matches = regex.Matches(line);
                foreach (Match match in matches)
                {
                    yield return new MatchPosition(match.Index, match.Length);
                }
            }
        }
    }
}