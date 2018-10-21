using System;
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
            _excludedExtensions = new[] {".exe", ".pdb", ".dll", ".db", ".idb", ".obj", ".uasset", ".ipch", ".cache", ".zip", ".rar", ".7z"};
        }

        public ExtensionFileSelector(string[] excludedExtensions)
        {
            _excludedExtensions = excludedExtensions;
        }

        protected override bool IsQueryMatching(FileInfo file) => true;
        protected override bool IsExtensionValid(FileInfo file) => !_excludedExtensions.Contains(file.Extension.ToLower());
    }
    
    public class QueryFileSelector : FileSelector
    {
        private readonly Regex _regex;

        public QueryFileSelector(string query)
        {
            _regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        
        protected override bool IsQueryMatching(FileInfo file)
        {
            if (String.IsNullOrEmpty(file?.FullName))
                return false;
            return _regex.IsMatch(file.FullName);
        }

        protected override bool IsExtensionValid(FileInfo file) => true;
    }
    
    public class QueryAndExtensionFileSelector : ExtensionFileSelector
    {
        private readonly Regex _regex;

        public QueryAndExtensionFileSelector(string query)
        {
            _regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        
        public QueryAndExtensionFileSelector(string query, string[] excludedExtensions)
            : base (excludedExtensions)
        {
            _regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        
        protected override bool IsQueryMatching(FileInfo file)
        {
            if (String.IsNullOrEmpty(file?.FullName))
                return false;
            return _regex.IsMatch(file.FullName);
        }
    }
}