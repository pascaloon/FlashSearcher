using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlashSearch
{
    public interface IFileSelector
    {
        bool IsFileValid(FileInfo file);
    }
    
    public class AnyFileSelector : IFileSelector
    {
        public bool IsFileValid(FileInfo file) => true;
    }

    public abstract class FileSelector : IFileSelector
    {
        protected abstract bool IsQueryMatching(FileInfo file);
        protected abstract bool IsExtensionValid(FileInfo file);
        public bool IsFileValid(FileInfo file) => IsExtensionValid(file) && IsQueryMatching(file);
    }
    

    public class ExtensionFileSelector : FileSelector
    {
        private readonly string[] _excludedExtensions;
        
        public ExtensionFileSelector()
        {
            _excludedExtensions = new[] {".exe", ".pdb", ".dll", ".db", ".idb", ".obj", ".uasset", ".ipch", ".cache"};
        }

        protected override bool IsQueryMatching(FileInfo file) => true;
        protected override bool IsExtensionValid(FileInfo file) => !_excludedExtensions.Contains(file.Extension.ToLower());
    }
    
    public class QueryFileSelector : FileSelector
    {
        private readonly Regex _regex;

        public QueryFileSelector(string query)
        {
            _regex = new Regex(query, RegexOptions.IgnoreCase);
        }
        
        protected override bool IsQueryMatching(FileInfo file) => _regex.IsMatch(file.FullName);
        protected override bool IsExtensionValid(FileInfo file) => true;
    }
    
    public class QueryAndExtensionFileSelector : ExtensionFileSelector
    {
        private readonly Regex _regex;

        public QueryAndExtensionFileSelector(string query)
        {
            _regex = new Regex(query, RegexOptions.IgnoreCase);
        }
        
        protected override bool IsQueryMatching(FileInfo file) => _regex.IsMatch(file.FullName);
    }
}