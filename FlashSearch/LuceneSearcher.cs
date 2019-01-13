using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            TopDocs docs = searcher.Search(query, Int32.MaxValue);
            
            using (IndexWriter indexWriter = new IndexWriter(
                new SimpleFSDirectory(_indexDirectory), 
                new StandardAnalyzer(Version.LUCENE_30), 
                false, 
                IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var topDoc in docs.ScoreDocs)
                {
                    if (_cancel)
                    {
                        yield break;
                    }
                    
                    Document doc = searcher.Doc(topDoc.Doc);
                    FileInfo file = new FileInfo(doc.GetField("Path").StringValue);

                    if (!fileSelector.IsFileValid(file))
                        continue;

                    if (!file.Exists)
                    {
                        indexWriter.DeleteDocuments(new Term("Path", file.FullName));
                        continue;
                    }
                    
                    yield return new SearchResult(
                        file, 
                        Int32.Parse(doc.GetField("LineNumber").StringValue), 
                        doc.GetField("LineContent").StringValue, 
                        long.Parse(doc.GetField("LastWrite").StringValue),
                        Enumerable.Empty<MatchPosition>());
                
                }
            }
            
            

        }


        public void IndexContentInFolder(string directoryPath, IFileSelector fileSelector)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Unable to find directory {directoryPath}");

            using (IndexWriter indexWriter = new IndexWriter(
                new SimpleFSDirectory(_indexDirectory), 
                new StandardAnalyzer(Version.LUCENE_30), 
                false, 
                IndexWriter.MaxFieldLength.UNLIMITED))
            using(Searcher searcher = new IndexSearcher(indexWriter.Directory))

            {
                IndexDirectory(indexWriter, searcher, new DirectoryInfo(directoryPath), fileSelector);
            }
        }

        private void IndexDirectory(IndexWriter indexWriter, Searcher searcher, DirectoryInfo directory, IFileSelector fileSelector)
        {
            if (!fileSelector.IsDirectoryValid(directory))
                return;
            
            try
            {

                foreach (DirectoryInfo childDir in directory.GetDirectories())
                {
                    if (_cancel)
                        return;
                    IndexDirectory(indexWriter, searcher, childDir, fileSelector);
                }

                foreach (FileInfo file in directory.GetFiles())
                {
                    if (!fileSelector.IsFileValid(file))
                        continue;
                    if (_cancel)
                        return;
                    IndexFile(indexWriter, searcher, file);
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
            long lastWriteTicks = file.LastWriteTime.Ticks;
            TopDocs topDocs = searcher.Search(new TermQuery(new Term("Path", file.FullName)), Int32.MaxValue);
            List<long> indexTicks = topDocs.ScoreDocs
                .Select(scoreDoc => searcher.Doc(scoreDoc.Doc))
                .Select(doc => long.Parse(doc.GetField("LastWrite").StringValue))
                .ToList();
            
            if (indexTicks.Any() && indexTicks.All(ticks => ticks == lastWriteTicks))
                return;

            indexWriter.DeleteDocuments(new Term("Path", file.FullName));
            int lineNumber = 1;
            foreach (string line in File.ReadLines(file.FullName))
            {
                Document document = new Document();
                document.Add(new Field("Path", file.FullName, Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("LineNumber", lineNumber.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("LineContent", line, Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("LastWrite", file.LastWriteTime.Ticks.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                indexWriter.AddDocument(document);
                ++lineNumber;
            }
        }
    }
}