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
        
        public RegexContentSelector(string pattern, bool caseSensitive)
        {
            RegexOptions regexOptions = RegexOptions.Compiled;
            if (!caseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;

            _regex = new Regex(pattern, regexOptions);
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
        public string LuceneQuery { get; protected set; }

        public LuceneContentSelector(string luceneQuery)
        {
            LuceneQuery = luceneQuery;
        }

        protected LuceneContentSelector()
        {
            LuceneQuery = String.Empty;
        }
        
        public virtual IEnumerable<MatchPosition> GetMatches(string line)
        {
            yield return new MatchPosition(0, line.Length);
        }
    }
    
    public class SmartContentSelector: LuceneContentSelector
    {
        public Regex RegexQuery { get; }

        public SmartContentSelector(string pattern, bool caseSensitive)
        {
            RegexOptions regexOptions = RegexOptions.Compiled;
            if (!caseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            RegexQuery = new Regex(pattern, regexOptions);
            
            Regex wordsRegex = new Regex(@"\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            LuceneQuery = "";
            string sanitized = Regex
                .Replace(pattern, @"\\\w", "")
                .Replace("_", " ")
                .Trim();
            foreach (Match match in wordsRegex.Matches(sanitized))
            {
                if (LuceneQuery.Length > 0)
                    LuceneQuery += " ";
                LuceneQuery += $"*{match.Value}*";
            }

        }
        
        public override IEnumerable<MatchPosition> GetMatches(string line)
        {
            MatchCollection matches = RegexQuery.Matches(line);
            foreach (Match match in matches)
            {
                yield return new MatchPosition(match.Index, match.Length);
            }
        }
    }
}