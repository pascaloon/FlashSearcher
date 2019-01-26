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
        public Regex RegexQuery { get; }

        public LuceneContentSelector(string regex)
        {
            RegexQuery = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            Regex wordsRegex = new Regex(@"\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            LuceneQuery = "";
            string sanitized = Regex
                .Replace(regex, @"\\\w", "")
                .Replace("_", " ")
                .Trim();
            foreach (Match match in wordsRegex.Matches(sanitized))
            {
                if (LuceneQuery.Length > 0)
                    LuceneQuery += " ";
                LuceneQuery += $"*{match.Value}*";
            }

        }
        
        public IEnumerable<MatchPosition> GetMatches(string line)
        {
            MatchCollection matches = RegexQuery.Matches(line);
            foreach (Match match in matches)
            {
                yield return new MatchPosition(match.Index, match.Length);
            }
        }
    }
}