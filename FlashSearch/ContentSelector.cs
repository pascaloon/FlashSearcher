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
}