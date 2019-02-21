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
        bool IsDirectoryValid(DirectoryInfo directory);
    }
    
    public class AnyFileSelector : IFileSelector
    {
        public bool IsFileValid(FileInfo file) => true;
        public bool IsDirectoryValid(DirectoryInfo directory) => true;
    }

    public class ExtensibleFileSelector : IFileSelector
    {
        private readonly List<Func<FileInfo, bool>> _filePredicates
            = new List<Func<FileInfo, bool>>();
        
        private readonly List<Func<DirectoryInfo, bool>> _directoryPredicates
            = new List<Func<DirectoryInfo, bool>>();
        
        internal ExtensibleFileSelector() { }
        
        public void AddFilePredicate(Func<FileInfo, bool> predicate)
        {
            _filePredicates.Add(predicate);
        }
        
        public void AddDirectoryPredicate(Func<DirectoryInfo, bool> predicate)
        {
            _directoryPredicates.Add(predicate);
        }
        
        public bool IsFileValid(FileInfo file) => _filePredicates.All(p => p(file));
        public bool IsDirectoryValid(DirectoryInfo directory) => _directoryPredicates.All(p => p(directory));
    }

    public class ExtensibleFileSelectorBuilder : 
        IBuilder<ExtensibleFileSelector>, 
        ExtensibleFileSelectorBuilder.IExcludedExtensions,
        ExtensibleFileSelectorBuilder.IExcludedPaths,
        ExtensibleFileSelectorBuilder.IRegex,
        ExtensibleFileSelectorBuilder.IExclusionRegex,
        ExtensibleFileSelectorBuilder.IMaxSize
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
            IExclusionRegex WithRegexFilter(string regex);
            IExclusionRegex WithoutRegexFilter();
        }
        
        public interface IExclusionRegex
        {
            IMaxSize WithExclusionRegex(string regex);
            IMaxSize WithoutExclusionRegex();
        }

        public interface IMaxSize
        {
            IBuilder<ExtensibleFileSelector> WithMaxSize(long size);
            IBuilder<ExtensibleFileSelector> WithoutMaxSize();
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
            _extensibleFileSelector.AddFilePredicate(f => !excludedExtensions.Contains(f.Extension.ToLower()));
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
            _extensibleFileSelector.AddDirectoryPredicate(f => !regexes.Any(r => r.IsMatch(f.FullName.EndsWith("\\") ? f.FullName : $"{f.FullName}\\")));
            return this;
        }

        public IRegex WithoutExcludedPaths()
        {
            return this;
        }
        
        public IExclusionRegex WithRegexFilter(string regex)
        {
            if (String.IsNullOrWhiteSpace(regex))
            {
                _extensibleFileSelector.AddFilePredicate(_ => true);
                return this;
            }
            Regex r = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _extensibleFileSelector.AddFilePredicate(f => r.IsMatch(f.FullName));
            return this;
        }

        public IExclusionRegex WithoutRegexFilter()
        {
            return this;
        }

        public IMaxSize WithExclusionRegex(string regex)
        {
            if (!String.IsNullOrWhiteSpace(regex))
            {
                Regex r = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                _extensibleFileSelector.AddFilePredicate(f => !r.IsMatch(f.FullName));    
            }
            return this;
        }

        public IMaxSize WithoutExclusionRegex()
        {
            return this;
        }

        public IBuilder<ExtensibleFileSelector> WithMaxSize(long size)
        {
            if (size > 0)
            {
                _extensibleFileSelector.AddFilePredicate(f => !f.Exists || f.Length <= size);
            }

            return this;
        }

        public IBuilder<ExtensibleFileSelector> WithoutMaxSize()
        {
            return this;
        }
    }
    
}