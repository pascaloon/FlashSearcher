using System;
using System.Collections.Generic;
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

    public class ExtensibleFileSelector : IFileSelector
    {
        private readonly List<Func<FileInfo, bool>> _predicates
            = new List<Func<FileInfo, bool>>();
        
        internal ExtensibleFileSelector() { }
        
        public void AddPredicate(Func<FileInfo, bool> predicate)
        {
            _predicates.Add(predicate);
        }
        
        public bool IsFileValid(FileInfo file) => _predicates.All(p => p(file));
        
    }

    public class ExtensibleFileSelectorBuilder : 
        IBuilder<ExtensibleFileSelector>, 
        ExtensibleFileSelectorBuilder.IExcludedExtensions,
        ExtensibleFileSelectorBuilder.IExcludedPaths,
        ExtensibleFileSelectorBuilder.IRegex
    {
        public interface IExcludedExtensions
        {
            IExcludedPaths WithExcludedExtensions(IEnumerable<string> excludedExtensions);
            IExcludedPaths WithoutExcludedExtensions();
        }
        
        public interface IExcludedPaths
        {
            IRegex WithExcludedPaths(IEnumerable<string> excludedPaths);
            IRegex WithoutExcludedPaths();
        }
        
        public interface IRegex
        {
            IBuilder<ExtensibleFileSelector> WithRegexFilter(string regex);
            IBuilder<ExtensibleFileSelector> WithoutRegexFilter();
        }

        private ExtensibleFileSelectorBuilder()
        {
            _extensibleFileSelector = new ExtensibleFileSelector();
        }

        private ExtensibleFileSelector _extensibleFileSelector;
        public static IExcludedExtensions NewFileSelector()
        {
            return new ExtensibleFileSelectorBuilder();
        }

        public ExtensibleFileSelector Build()
        {
            return _extensibleFileSelector;
        }

        public IExcludedPaths WithExcludedExtensions(IEnumerable<string> excludedExtensions)
        {
            _extensibleFileSelector.AddPredicate(f => !excludedExtensions.Contains(f.Extension.ToLower()));
            return this;
        }

        public IExcludedPaths WithoutExcludedExtensions()
        {
            return this;
        }
        
        public IRegex WithExcludedPaths(IEnumerable<string> excludedPaths)
        {
            List<Regex> regexes = excludedPaths
                .Where(p => !String.IsNullOrWhiteSpace(p))
                .Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase))
                .ToList();
            _extensibleFileSelector.AddPredicate(f => !regexes.Any(r => r.IsMatch(f.FullName)));
            return this;
        }

        public IRegex WithoutExcludedPaths()
        {
            return this;
        }
        
        public IBuilder<ExtensibleFileSelector> WithRegexFilter(string regex)
        {
            Regex r = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _extensibleFileSelector.AddPredicate(f => r.IsMatch(f.FullName));
            return this;
        }

        public IBuilder<ExtensibleFileSelector> WithoutRegexFilter()
        {
            return this;
        }

    }
    
}