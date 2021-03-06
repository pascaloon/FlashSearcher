using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using Version = Lucene.Net.Util.Version;


namespace FlashSearch
{   
    public class LuceneSearcher
    {
        private readonly DirectoryInfo _indexDirectory;

        private ConcurrentQueue<FileInfo> _indexableFiles;
        
        public long FilesIndexed { get; private set; }        
        public long TotalFilesFound { get; private set; }
        public int MaxSearchResults { get; set; } = 10000; 

        public LuceneSearcher(string indexPath)
        {
            if (!Directory.Exists(indexPath))
            {
                Directory.CreateDirectory(indexPath);
            }

            _indexDirectory = new DirectoryInfo(indexPath);

            
            if (!_indexDirectory.GetFiles().Any())
            {
                using (IndexWriter indexWriter = new IndexWriter(
                    new SimpleFSDirectory(_indexDirectory), 
                    new StandardAnalyzer(Version.LUCENE_30), 
                    true, 
                    IndexWriter.MaxFieldLength.UNLIMITED))
                { }
            }
            
        }

        private bool _cancel = false;
        public void CancelSearch()
        {
            _cancel = true;
        }
        
        public IEnumerable<SearchResult> SearchContentInFolder(string directoryPath, IFileSelector fileSelector, LuceneContentSelector luceneQuery)
        {
            _cancel = false;
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Unable to find directory {directoryPath}");
            
            Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);
            Searcher searcher = new IndexSearcher(Lucene.Net.Store.FSDirectory.Open(_indexDirectory));
            QueryParser queryParser = new QueryParser(Version.LUCENE_30, "LineContent", analyzer);
            queryParser.AllowLeadingWildcard = true;
            Query query = queryParser.Parse(luceneQuery.LuceneQuery);
            TopDocs docs = searcher.Search(query, MaxSearchResults);

            foreach (var topDoc in docs.ScoreDocs)
            {
                if (_cancel)
                {
                    yield break;
                }

                Document doc = searcher.Doc(topDoc.Doc);
                FileInfo file = new FileInfo(doc.GetField("Path").StringValue);

                if (!file.FullName.StartsWith(directoryPath, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (!fileSelector.IsFileValid(file) || !fileSelector.IsDirectoryValid(file.Directory))
                    continue;

                string lineContent = doc.GetField("LineContent").StringValue;
                List<MatchPosition> matches = luceneQuery.GetMatches(lineContent).ToList();

                if (matches.Count == 0)
                    continue;

                yield return new SearchResult(
                    file,
                    Int32.Parse(doc.GetField("LineNumber").StringValue),
                    lineContent,
                    long.Parse(doc.GetField("LastWrite").StringValue),
                    matches);

            }

        }


        private bool _ioCompleted = false;
        
        public void IndexContentInFolder(string directoryPath, IFileSelector fileSelector)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Unable to find directory {directoryPath}");
            
            _indexableFiles = new ConcurrentQueue<FileInfo>();
            FilesIndexed = 0;
            TotalFilesFound = 0;
            
            _ioCompleted = false;
            CancellationTokenSource cancelIndexingTokenSource = new CancellationTokenSource();
            CancellationToken cancelIndexingToken = cancelIndexingTokenSource.Token;
            
            using (IndexWriter indexWriter = new IndexWriter(
                new SimpleFSDirectory(_indexDirectory), 
                new StandardAnalyzer(Version.LUCENE_30), 
                false, 
                IndexWriter.MaxFieldLength.UNLIMITED))
            using(Searcher searcher = new IndexSearcher(indexWriter.Directory))
            {
                
                Task indexDirectoryTask = Task.Run(() =>
                    {
                        cancelIndexingToken.ThrowIfCancellationRequested();
                        IndexDirectory(new DirectoryInfo(directoryPath), fileSelector);
                    }, cancelIndexingToken);

                HashSet<string> allKnownPaths = new HashSet<string>();

                TopDocs allTopDocs = searcher.Search(new MatchAllDocsQuery(), MaxSearchResults);
                foreach (ScoreDoc scoreDoc in allTopDocs.ScoreDocs)
                {
                    if (_cancel)
                        break;
                    
                    Document doc = searcher.Doc(scoreDoc.Doc);
                    String path = doc.GetField("Path").StringValue;
                    if (allKnownPaths.Contains(path))
                        continue;

                    FileInfo fileInfo = new FileInfo(path);
                    
                    if (!fileSelector.IsFileValid(fileInfo) || !fileSelector.IsDirectoryValid(fileInfo.Directory))
                        continue;
                    
                    long lastWrite = long.Parse(doc.GetField("LastWrite").StringValue);
                    
                    if (fileInfo.Exists)
                    {
                        IndexFile(indexWriter, searcher, fileInfo, lastWrite);
                        allKnownPaths.Add(path);
                    }
                    else
                    {
                        var query = new PhraseQuery();
                        query.Add(new Term("Path", path));
                        indexWriter.DeleteDocuments(query);
                    }
                    
                    ++FilesIndexed;
                }

                // How many indexers to spawn at first
                int initialIndexersCount = 2;
                // How much files must be queued before spawning help
                int indexersScalingFactor = 1000;
                // Max amount of indexers
                int maxIndexersCount = 5;
                
                List<Thread> threads = new List<Thread>();
                for (int i = 0; i < initialIndexersCount; i++)
                {
                    Thread t = new Thread(() => IndexLoop(indexWriter, searcher, allKnownPaths));
                    t.Start();
                    threads.Add(t);
                }

                while (true)
                {
                    if (_cancel)
                    {
                        cancelIndexingTokenSource.Cancel();
                        _ioCompleted = true;
                        break;
                    }

                    if (indexDirectoryTask.IsCompleted)
                    {
                        _ioCompleted = true;
                        break;
                    }

                    if (threads.Count < maxIndexersCount && _indexableFiles.Count / threads.Count >= indexersScalingFactor)
                    {
                        Thread t = new Thread(() => IndexLoop(indexWriter, searcher, allKnownPaths));
                        t.Start();
                        threads.Add(t);
                    }
                    
                    Thread.Sleep(100);
                }
                
                foreach (Thread thread in threads)
                {
                    thread.Join();
                }
            }
        }

        private void IndexLoop(IndexWriter indexWriter, Searcher searcher, HashSet<String> allKnownPaths)
        {
            while (_indexableFiles.Any() || !_ioCompleted)
            {
                if (_cancel)
                {
                    break;
                }
                
                if (_indexableFiles.TryDequeue(out FileInfo r))
                {
                    if (!allKnownPaths.Contains(r.FullName))
                    {
                        IndexFile(indexWriter, searcher, r);
                        ++FilesIndexed;
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }

                if (!_indexableFiles.Any())
                {
                    Thread.Sleep(100);
                }
                    
            }
        }
        

        private void IndexDirectory(DirectoryInfo directory, IFileSelector fileSelector)
        {
            if (!fileSelector.IsDirectoryValid(directory))
                return;
            
            try
            {

                foreach (DirectoryInfo childDir in directory.GetDirectories())
                {
                    if (_cancel)
                        return;
                    IndexDirectory(childDir, fileSelector);
                }

                foreach (FileInfo file in directory.GetFiles())
                {
                    if (!fileSelector.IsFileValid(file))
                        continue;
                    if (_cancel)
                        return;
                    _indexableFiles.Enqueue(file);
                    ++TotalFilesFound;
                }
                
            }
            catch (Exception)
            {
                // TODO: Add better handling of error and logging. Maybe add NLog support ?
                Console.WriteLine("Error while indexing directory: {0}", directory.FullName);
            }
        }

        private void IndexFile(IndexWriter indexWriter, Searcher searcher, FileInfo file)
        {
            var query = new PhraseQuery();
            query.Add(new Term("Path", file.FullName));
            TopDocs topDocs = searcher.Search(query, 1);
            IEnumerable<long> indexTicks = topDocs.ScoreDocs
                .Select(scoreDoc => searcher.Doc(scoreDoc.Doc))
                .Select(doc => long.Parse(doc.GetField("LastWrite").StringValue));

            IndexFile(indexWriter, searcher, file, indexTicks.FirstOrDefault());
        }
        
        private void IndexFile(IndexWriter indexWriter, Searcher searcher, FileInfo file, long lastWrite)
        {
            long lastWriteTicks = file.LastWriteTime.Ticks;
            if (lastWrite == lastWriteTicks)
                return;

            var query = new PhraseQuery();
            query.Add(new Term("Path", file.FullName));
            indexWriter.DeleteDocuments(query);
            int lineNumber = 1;
            foreach (string line in File.ReadLines(file.FullName))
            {
                if (IsPertinentLine(line))
                {
                    Document document = new Document();
                    document.Add(new Field("Path", file.FullName, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    document.Add(new Field("LineNumber", lineNumber.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    document.Add(new Field("LineContent", line, Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("LastWrite", lastWriteTicks.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    indexWriter.AddDocument(document);
                }
                
                ++lineNumber;
            }
        }

        private bool IsPertinentLine(string line)
        {
            // We don't care about lines with only WhiteSpace
            if (String.IsNullOrWhiteSpace(line))
                return false;
            
            string trimmedLine = line.Trim();
            
            // We care about lines that are more than 1 character
            if (trimmedLine.Length > 1)
                return true;
            
            // If the line is only 1 character, we don't care about braces.
            switch (trimmedLine)
            {
                case "{":
                case "}":
                    return false;
                default:
                    return true;
            }
        }
    }
}