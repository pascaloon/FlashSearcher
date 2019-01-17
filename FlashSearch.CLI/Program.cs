using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FlashSearch.Configuration;

namespace FlashSearch.CLI
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            int argsCount = args.Length;
            if (argsCount < 1 || argsCount > 3)
            {
                PrintHelp();
                return;
            }

            if (argsCount == 1 && String.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return;
            }
            
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            
            var watcher = new ConfigurationWatcher<SearchConfiguration>(
                GetConfigurationPath(),
                XMLIO.Load<SearchConfiguration>,
                XMLIO.Save,
                () => SearchConfiguration.Default);
            SearchConfiguration searchConfiguration = watcher.GetConfiguration();

            string path = Directory.GetCurrentDirectory();
            var fileSelectorBuilder = ExtensibleFileSelectorBuilder.NewFileSelector();
            ExtensibleFileSelector fileSelector;
            string queryRegex;

            switch (argsCount)
            {
                case 1:
                    fileSelector = fileSelectorBuilder
                        .WithExcludedExtensions(searchConfiguration.ExcludedExtensions)
                        .WithExcludedPaths(searchConfiguration.ExcludedPaths)
                        .WithoutRegexFilter()
                        .WithoutExclusionRegex()
                        .Build();
                    queryRegex = args[0];
                    break;
                case 2:
                    fileSelector = fileSelectorBuilder
                        .WithExcludedExtensions(searchConfiguration.ExcludedExtensions)
                        .WithExcludedPaths(searchConfiguration.ExcludedPaths)
                        .WithRegexFilter(args[0])
                        .WithoutExclusionRegex()
                        .Build();
                    queryRegex = args[1];
                    break;
                case 3:
                default:
                    path = Path.IsPathRooted(args[0]) 
                        ? args[0] 
                        : Path.GetFullPath(Path.Combine(path, args[0]));
                    fileSelector = fileSelectorBuilder
                        .WithExcludedExtensions(searchConfiguration.ExcludedExtensions)
                        .WithExcludedPaths(searchConfiguration.ExcludedPaths)
                        .WithRegexFilter(args[1])
                        .WithoutExclusionRegex()
                        .Build();
                    queryRegex = args[2];
                    break;
            }
            
            IContentSelector contentSelector = new RegexContentSelector(queryRegex);
            var flashSearcher = new FlashSearcher();
            foreach (var result in flashSearcher.SearchContentInFolder(path, fileSelector, contentSelector))
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
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();
            System.Environment.Exit(0);
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Invalid Arguments. Expected: [ [ PATH ] FILENAME_REGEX ] QUERY_REGEX");
        }
        
        private static string GetConfigurationPath()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(directoryName))
                throw new Exception("Unable to find executable's directory.");
            return Path.Combine(directoryName, "SearchConfiguration.xml");
        }
    }
}