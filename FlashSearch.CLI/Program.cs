﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using FlashSearch.Configuration;

namespace FlashSearch.CLI
{
    [Verb("search", HelpText = "Search in subfolders with the specified query.")]
    internal class SearchOptions
    {
        [Option('f', "filter", Default = null, Required = false, HelpText = "Filter searched file with the specified regex filter.")]
        public string FileFilter { get; set; }
        [Option('t', "filetype", Default = null, Required = false, HelpText = "Filter searched file with the specified filter name from the config file.")]
        public string FileFilterName { get; set; }
        [Option('q', "query", Required = true, HelpText = "Regex to search content.")]
        public string Query { get; set; }
    }
    
    [Verb("lucene-search", HelpText = "Search in the specified project's index for the given query.")]
    internal class LuceneSearchOptions
    {
        [Option('t', "filetype", Required = true, HelpText = "Filter searched file with the specified filter name from the config file.")]
        public string FileFilterName { get; set; }
        [Option('q', "query", Required = true, HelpText = "Regex to search content.")]
        public string Query { get; set; }
    }

    [Verb("lucene-index", HelpText = "Index the filtered files to the specified index.")]
    internal class LuceneIndexOptions
    {
        [Option('t', "filetype", Required = true, HelpText = "Filter searched file with the specified filter name from the config file.")]
        public string FileFilterName { get; set; }
    }
    
    internal class Program
    {
        private static String ConfigFileName = "SearchConfiguration.xml";
        
        private static SearchConfiguration LoadConfiguration()
        {
            var watcher = new ConfigurationWatcher<SearchConfiguration>(
                GetConfigurationPath(),
                XMLIO.Load<SearchConfiguration>,
                XMLIO.Save,
                () => SearchConfiguration.Default);
            return watcher.GetConfiguration();
        }

        private static void PrintSearchResult(SearchResult result)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{result.FileInfo.FullName}:{result.LineNumber}: ");
            Console.ResetColor();

            List<MatchPosition> positions = result.MatchPositions.ToList();
            for (int i = 0; i < positions.Count; i++)
            {
                int lastIndex = i == 0 ? 0 : (positions[i - 1].Begin + positions[i - 1].Length);
                string before = result.LineContent.Substring(lastIndex, positions[i].Begin - lastIndex);
                string match = result.LineContent.Substring(positions[i].Begin, positions[i].Length);
                    
                Console.Write(before);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(match);
                Console.ResetColor();
            }
            string after = result.LineContent.Substring(positions.Last().Begin + positions.Last().Length);
            Console.WriteLine(after);
        }
        
        private static int Search(SearchOptions options)
        {
            SearchConfiguration config = LoadConfiguration();
            string path = Directory.GetCurrentDirectory();        
            
            IFileSelector fileSelector = null;
            if (options.FileFilter != null && options.FileFilterName == null)
            {
                fileSelector = ExtensibleFileSelectorBuilder.NewFileSelector()
                    .WithoutExcludedExtensions()
                    .WithExcludedPaths(config.ExcludedPaths)
                    .WithRegexFilter(options.FileFilter)
                    .WithoutExclusionRegex()
                    .WithMaxSize(config.MaxFileSize)
                    .Build();
            } 
            else if (options.FileFilter == null && options.FileFilterName != null)
            {
                FileFilter fileFilter = config.FileFilters.First(filter =>
                    filter.Name.Equals(options.FileFilterName, StringComparison.InvariantCultureIgnoreCase));
                if (fileFilter == null)
                    throw new ArgumentException($"File filter named '{options.FileFilterName}' does not exist. Please edit '{ConfigFileName}'.");
                
                fileSelector = ExtensibleFileSelectorBuilder.NewFileSelector()
                    .WithoutExcludedExtensions()
                    .WithExcludedPaths(config.ExcludedPaths)
                    .WithRegexFilter(fileFilter.Regex)
                    .WithExclusionRegex(fileFilter.Exclusion)
                    .WithMaxSize(config.MaxFileSize)
                    .Build();
            }
            else
            {
                fileSelector = ExtensibleFileSelectorBuilder.NewFileSelector()
                    .WithExcludedExtensions(config.ExcludedExtensions)
                    .WithExcludedPaths(config.ExcludedPaths)
                    .WithoutRegexFilter()
                    .WithoutExclusionRegex()
                    .WithoutMaxSize()
                    .Build();
            }
            
            IContentSelector contentSelector = new RegexContentSelector(options.Query);
            var flashSearcher = new FlashSearcher();
            foreach (var result in flashSearcher.SearchContentInFolder(path, fileSelector, contentSelector))
            {
                PrintSearchResult(result);
            }
            
            
            return 0;
        }
        
        private static int LuceneSearch(LuceneSearchOptions options)
        {
            SearchConfiguration config = LoadConfiguration();
            
            string path = Directory.GetCurrentDirectory();
            Project project = config.Projects.FirstOrDefault(
                p => path.StartsWith(p.Path, StringComparison.InvariantCultureIgnoreCase));

            if (project == null)
                throw new ArgumentException($"Current path is not under any known project. Please edit '{ConfigFileName}'.");


            FileFilter fileFilter = config.FileFilters.FirstOrDefault(filter =>
                filter.Name.Equals(options.FileFilterName, StringComparison.InvariantCultureIgnoreCase));

            if (fileFilter == null)
                throw new ArgumentException($"File filter named '{options.FileFilterName}' does not exist. Please edit '{ConfigFileName}'.");

            var fileSelector = ExtensibleFileSelectorBuilder.NewFileSelector()
                .WithExcludedExtensions(config.ExcludedExtensions)
                .WithExcludedPaths(config.ExcludedPaths)
                .WithRegexFilter(fileFilter.Regex)
                .WithExclusionRegex(fileFilter.Exclusion)
                .WithMaxSize(config.MaxFileSize)
                .Build();

            LuceneContentSelector contentSelector = new LuceneContentSelector(options.Query);
            
            string localDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string indexDirectory = Path.Combine(localDirectory, "Indexes", project.Name, fileFilter.Index);
            var luceneSearcher = new LuceneSearcher(indexDirectory) {MaxSearchResults = Int32.MaxValue};
            foreach (var result in luceneSearcher.SearchContentInFolder(path, fileSelector, contentSelector))
            {
                PrintSearchResult(result);
            }

            return 0;
        }
        
        private static int LuceneIndex(LuceneIndexOptions options)
        {            
            SearchConfiguration config = LoadConfiguration();
            
            string path = Directory.GetCurrentDirectory();
            Project project = config.Projects.FirstOrDefault(
                p => path.StartsWith(p.Path, StringComparison.InvariantCultureIgnoreCase));

            if (project == null)
                throw new ArgumentException($"Current path is not under any known project. Please edit '{ConfigFileName}'.");

            FileFilter fileFilter = config.FileFilters.FirstOrDefault(filter =>
                filter.Name.Equals(options.FileFilterName, StringComparison.InvariantCultureIgnoreCase));

            if (fileFilter == null)
                throw new ArgumentException($"File filter named '{options.FileFilterName}' does not exist. Please edit '{ConfigFileName}'.");

            var fileSelector = ExtensibleFileSelectorBuilder.NewFileSelector()
                .WithExcludedExtensions(config.ExcludedExtensions)
                .WithExcludedPaths(config.ExcludedPaths)
                .WithRegexFilter(fileFilter.Regex)
                .WithExclusionRegex(fileFilter.Exclusion)
                .WithMaxSize(config.MaxFileSize)
                .Build();
            
            string localDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string indexDirectory = Path.Combine(localDirectory, "Indexes", project.Name, fileFilter.Index);
            var luceneSearcher = new LuceneSearcher(indexDirectory);

            Console.Write($"Indexing {path}...");
            Console.Write($" Done.");
            
            luceneSearcher.IndexContentInFolder(path, fileSelector);
            
            return 0;            
        }
        
        public static int Main(string[] args)
        {
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            try
            {
                Parser.Default.ParseArguments<SearchOptions, LuceneSearchOptions, LuceneIndexOptions>(args)
                    .MapResult(
                        (SearchOptions o) => Search(o),
                        (LuceneSearchOptions o) => LuceneSearch(o),
                        (LuceneIndexOptions o) => LuceneIndex(o),
                        errors => 1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
            
            return 0;
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();
            System.Environment.Exit(0);
        }
        
        private static string GetConfigurationPath()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(directoryName))
                throw new Exception("Unable to find executable's directory.");
            return Path.Combine(directoryName, ConfigFileName);
        }
    }
}