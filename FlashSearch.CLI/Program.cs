using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using FlashSearch.Configuration;

namespace FlashSearch.CLI
{
    internal class CommonSearchOptions
    {
        [Option("format", Default = "%F:%n: %l", Required = false, HelpText = "Format of output results")]
        public string Format { get; set; }
    }
    
    [Verb("search", HelpText = "Search in subfolders with the specified query.")]
    internal class SearchOptions : CommonSearchOptions
    {
        [Option('f', "filter", Default = null, Required = false, HelpText = "Filter searched file with the specified regex filter.")]
        public string FileFilter { get; set; }
        [Option('t', "filetype", Default = null, Required = false, HelpText = "Filter searched file with the specified filter name from the config file.")]
        public string FileFilterName { get; set; }
        [Option('q', "query", Required = true, HelpText = "Regex to search content.")]
        public string Query { get; set; }
    }
    
    [Verb("smart-search", HelpText = "Search in the specified project's index for the given query.")]
    internal class SmartSearchOptions : CommonSearchOptions
    {
        [Option('t', "filetype", Required = true, HelpText = "Filter searched file with the specified filter name from the config file.")]
        public string FileFilterName { get; set; }
        [Option('q', "query", Required = true, HelpText = "Regex to search content.")]
        public string Query { get; set; }
    }
    
    [Verb("lucene-search", HelpText = "Search in the specified project's index for the given query.")]
    internal class LuceneSearchOptions : CommonSearchOptions
    {
        [Option('t', "filetype", Required = true, HelpText = "Filter searched file with the specified filter name from the config file.")]
        public string FileFilterName { get; set; }
        [Option('q', "query", Required = true, HelpText = "Lucene query to search content.")]
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
        private static readonly String ConfigFileName = "SearchConfiguration.xml";
        private static ConsoleWriter _console;
        
        private static SearchConfiguration LoadConfiguration()
        {
            var watcher = new ConfigurationWatcher<SearchConfiguration>(
                GetConfigurationPath(),
                XMLIO.Load<SearchConfiguration>,
                XMLIO.Save,
                () => SearchConfiguration.Default);
            return watcher.GetConfiguration();
        }

        private static void InitSearch(CommonSearchOptions options)
        {
            _console = new ConsoleWriter(options.Format);
        }
        
        private static int Search(SearchOptions options)
        {
            InitSearch(options);
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
                _console.PrintSearchResult(result);
            }
            
            
            return 0;
        }
        
        private static int SmartSearch(SmartSearchOptions options)
        {
            InitSearch(options);
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

            SmartContentSelector contentSelector = new SmartContentSelector(options.Query);
            
            string localDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string indexDirectory = Path.Combine(localDirectory, "Indexes", project.Name, fileFilter.Index);
            var luceneSearcher = new LuceneSearcher(indexDirectory) {MaxSearchResults = Int32.MaxValue};
            foreach (var result in luceneSearcher.SearchContentInFolder(path, fileSelector, contentSelector))
            {
                _console.PrintSearchResult(result);
            }

            return 0;
        }
        
        private static int LuceneSearch(LuceneSearchOptions options)
        {
            InitSearch(options);
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
                _console.PrintSearchResult(result);
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
            luceneSearcher.IndexContentInFolder(path, fileSelector);
            Console.WriteLine($" Done.");

            return 0;            
        }
        
        public static int Main(string[] args)
        {
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            
            try
            {
                Parser.Default.ParseArguments<SearchOptions, SmartSearchOptions, LuceneSearchOptions, LuceneIndexOptions>(args)
                    .MapResult(
                        (SearchOptions o) => Search(o),
                        (SmartSearchOptions o) => SmartSearch(o),
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
            _console.CanWrite = false;
            _console.Write("\nSearch cancelled by user.\n", ConsoleColor.Red, true);
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
    
    class ConsoleWriter
    {
        private Dictionary<char, Action<SearchResult>> _formatKeys;
        
        public bool CanWrite { get; set; }

        private List<Action<SearchResult>> _outputParts;
        
        public ConsoleWriter(string format)
        {
            CanWrite = true;
            
            _formatKeys = new Dictionary<char, Action<SearchResult>>()
            {
                {'F', r => Write(r.FileInfo.FullName, ConsoleColor.DarkGray)},
                {'f', r => Write(r.FileInfo.FullName.Replace(Directory.GetCurrentDirectory(), "."), ConsoleColor.DarkGray)},
                {'n', r => Write(r.LineNumber.ToString(), ConsoleColor.DarkGray)},
                {'l', WriteMatchedLine},
                {'m', WriteMatch},
            };
            
            _outputParts = new List<Action<SearchResult>>();

            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '%')
                {
                    if (i + 1 == format.Length)
                        throw new FormatException("Special character '%' incomplete.");
                    else if (format[i + 1] == '%')
                        i++; // skip it: %% is escaped to %
                    else if (!_formatKeys.ContainsKey(format[i+1]))
                        throw new FormatException($"Special character '%{format[i+1]}' is not recognized.");
                    else
                    {
                        if (buffer.Length != 0)
                        {
                            string b = buffer.ToString();
                            _outputParts.Add(r => Write(b, ConsoleColor.DarkGray));
                        }
                        _outputParts.Add(_formatKeys[format[i+1]]);
                        buffer.Clear();
                        i++;
                    }
                }
                else
                {
                    buffer.Append(format[i]);
                }
            }
            
            if (buffer.Length != 0)
            {
                string b = buffer.ToString();
                _outputParts.Add(r => Write(b, ConsoleColor.DarkGray));
            }

            _outputParts.Add(r => Write("\n", null));

        }
        
        public void Write(string text, ConsoleColor? color, bool force = false)
        {
            if (!force && !CanWrite)
                return;
            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
            }
            else
            {                
                Console.ResetColor();
            }
            
            Console.Write(text);
        }

        private void WriteMatchedLine(SearchResult result)
        {
            List<MatchPosition> positions = result.MatchPositions.ToList();
            for (int i = 0; i < positions.Count; i++)
            {
                int lastIndex = i == 0 ? 0 : (positions[i - 1].Begin + positions[i - 1].Length);
                string before = result.LineContent.Substring(lastIndex, positions[i].Begin - lastIndex);
                string match = result.LineContent.Substring(positions[i].Begin, positions[i].Length);
                    
                Write(before, null);
                Write(match, ConsoleColor.Green);
            }
            string after = result.LineContent.Substring(positions.Last().Begin + positions.Last().Length);
            Write(after, null);
        }
        
        private void WriteMatch(SearchResult result)
        {
            MatchPosition firstMatch = result.MatchPositions.First();                    
            Write(result.LineContent.Substring(firstMatch.Begin, firstMatch.Length), ConsoleColor.Green);
        }
        
        public void PrintSearchResult(SearchResult result)
        {
            foreach (var outputPart in _outputParts)
            {
                outputPart(result);
            }         
            
        }
    }
    
}