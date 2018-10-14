using System;
using System.Collections.Generic;
using System.Linq;

namespace FlashSearch.CLI
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Invalid Arguments. Expected: PATH REGEX");
                return;
            }

            var flashSearcher = new FlashSearcher(SearchConfiguration.Default);
            foreach (var result in flashSearcher.SearchContentInFolder(args[0], args[1]))
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
    }
}