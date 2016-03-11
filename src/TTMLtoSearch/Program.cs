using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace TTMLtoSearch
{
    class Program
    {
        private static string searchServiceName = [azure search service];
        private static string apiKey = [azure search service api key];
        private static SearchServiceClient _searchClient;
        private static SearchIndexClient _indexClient;
        private static string AzureSearchIndex = "buildsessions";

        static void Main(string[] args)
        {

            // Create an HTTP reference to the catalog index
            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            _indexClient = _searchClient.Indexes.GetClient(AzureSearchIndex);

            Console.WriteLine("{0}", "Deleting index...\n");
            if (DeleteIndex())
            {
                Console.WriteLine("{0}", "Creating index...\n");
                CreateIndex();
            }

            Console.WriteLine("{0}", "Uploading video metadata...\n");
            UploadMetadata(_indexClient);

            // Execute a search for the term 'Azure Search' which will only return one result
            Console.WriteLine("{0}", "Searching for videos about 'Azure Search'...\n");
            DocumentSearchResult results = SearchIndex("'Azure Search'");
            foreach (var doc in results.Results)
                Console.WriteLine("Found Session: {0}", doc.Document["session_title"]);

            Console.WriteLine("{0}", "\nMerging in transcribed text from videos...\n");
            MergeTranscribedText(_indexClient);

            // Execute a search for the term 'Azure Search' which will return multiple results
            Console.WriteLine("{0}", "Searching for videos about 'Azure Search'...\n");
            results = SearchIndex("'Azure Search'");
            foreach (var doc in results.Results)
                Console.WriteLine("Found Session: {0}", doc.Document["session_title"]);

            Console.WriteLine("\nPress any key to continue\n");
            Console.ReadLine();

        }

        private static bool DeleteIndex()
        {
            // Delete the index, data source, and indexer.
            try
            {
                _searchClient.Indexes.Delete(AzureSearchIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting index: {0}\r\n", ex.Message);
                Console.WriteLine("Did you remember to add your SearchServiceName and SearchServiceApiKey to the app.config?\r\n");
                return false;
            }

            return true;
        }
        private static void CreateIndex()
        {
            // Create the Azure Search index based on the included schema
            try
            {
                var definition = new Index()
                {
                    Name = AzureSearchIndex,
                    Fields = new[] 
                    { 
                        new Field("session_id",     DataType.String)         { IsKey = true,  IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("session_title",  DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
                        new Field("tags",           DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
                        new Field("speakers",       DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
                        //new Field("tags",           DataType.Collection(DataType.String))     { IsSearchable = true, IsFilterable = true, IsFacetable = true },
                        //new Field("speakers",       DataType.Collection(DataType.String))     { IsSearchable = true, IsFilterable = true, IsFacetable = true },
                        new Field("date",           DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true},
                        new Field("url",            DataType.String)         { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("transcribed_text",DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false,  IsSortable = false,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
                    }
                };
                _searchClient.Indexes.Create(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}\r\n", ex.Message);
            }

        }

        private static void UploadMetadata(SearchIndexClient indexClient)
        {
            // Upload metadata on Build Sessions from a CSV file
            List<IndexAction> indexOperations = GetSessionsFromCSV(); 

            try
            {
                indexClient.Documents.Index(new IndexBatch(indexOperations));
            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine(
                 "Failed to index some of the documents: {0}",e.Message);
            }

            // Wait a while for indexing to complete.
            Console.WriteLine("{0}", "Waiting 5 seconds for content to become searchable...\n");
            Thread.Sleep(5000);
        }

        private static void MergeTranscribedText(SearchIndexClient indexClient)
        {
            // Upload metadata on Build Sessions from a CSV file
            List<IndexAction> indexOperations = new List<IndexAction>();
            string[] files = Directory.GetFiles(@"ttml");
            int counter = 0;
            try
            {
                foreach (var file in files)
                {
                    Document doc = new Document();

                    string session_id = file.Substring(file.IndexOf("\\") + 1).Replace(".mp3.ttml", "").ToLower();
                    doc.Add("session_id", ConvertToAlphaNumeric(session_id));
                    doc.Add("transcribed_text", ParseTTML(file));
                    indexOperations.Add(IndexAction.MergeOrUpload(doc));
                    counter++;
                    if (counter >= 100)
                    {
                        Console.WriteLine("Indexing {0} transcriptions...\n", counter);
                        indexClient.Documents.Index(new IndexBatch(indexOperations));
                        indexOperations.Clear();
                        counter = 0;
                    }
                }
                if (counter > 0)
                {
                    Console.WriteLine("Indexing {0} transcriptions...\n", counter);
                    indexClient.Documents.Index(new IndexBatch(indexOperations));
                }

            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine(
                 "Failed to index some of the documents: {0}", e.Message);

                //Console.WriteLine(
                // "Failed to index some of the documents: {0}",
                //        String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));


            }

            // Wait a while for indexing to complete.
            Console.WriteLine("{0}", "Waiting 5 seconds for content to become searchable...\n");
            Thread.Sleep(5000);
        }



        private static DocumentSearchResult SearchIndex(string searchText)
        {
            // Execute search based on query string
            try
            {
                SearchParameters sp = new SearchParameters() { SearchMode = SearchMode.All };
                return _indexClient.Documents.Search(searchText, sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }
        private static List<IndexAction> GetSessionsFromCSV()
        {
            List<IndexAction> indexOperations = new List<IndexAction>();

            //Data provided by http://download.geonames.org/export/zip/
            //This work is licensed under a Creative Commons Attribution 3.0 License.
            //This means you can use the dump as long as you give credit to geonames (a link on your website to www.geonames.org is ok)
            //see http://creativecommons.org/licenses/by/3.0/

            using (OleDbConnection cn = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Directory.GetCurrentDirectory() + ";" + "Extended Properties=\"Text;HDR=No;FMT=Delimited;\""))
            {
                cn.Open();
                using (OleDbCommand cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM [BuildSessionMetatdata.csv]";
                    cmd.CommandType = CommandType.Text;
                    int counter = 0;
                    using (OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        foreach (DbDataRecord record in reader)
                        {
                            counter++;

                            Document doc = new Document();
                            string title = record.GetString(0);
                            string session_id = title.Replace(" ", "_").ToLower();
                            doc.Add("session_id", ConvertToAlphaNumeric(session_id));
                            doc.Add("session_title", title);
                            doc.Add("tags", record.GetValue(1).ToString() == "" ? "" : record.GetString(1));
                            doc.Add("speakers", record.GetValue(2).ToString() == "" ? "" : record.GetString(2));
                            doc.Add("date", Convert.ToDateTime(record.GetValue(3)).ToShortDateString());
                            doc.Add("url", record.GetValue(4).ToString() == "" ? "" : record.GetString(4));
                            indexOperations.Add(IndexAction.Upload(doc));
                        }

                    }
                }
            }
            return indexOperations;
        }

        static string ConvertToAlphaNumeric(string plainText)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(plainText, "");
        }
        static string ParseTTML(string ttmlFile)
        {
            // This will extract all the spoken text from a TTML file into a single string
            string content = string.Empty;
            string parsedLine;
            try
            {
                // Read line by line starting to get content after <body region="CaptionArea"><div>
                string line;
                bool foundContent = false;
                System.IO.StreamReader file = new System.IO.StreamReader(ttmlFile);
                while ((line = file.ReadLine()) != null)
                {
                    if (line.IndexOf("<body region=\"CaptionArea\">") > -1)
                        foundContent = true;
                    else if (line.IndexOf("</body>") > -1)
                        foundContent = false;
                    else if ((foundContent) && (line.IndexOf("<p begin") > -1))
                    {
                        parsedLine = line.Substring(line.IndexOf(">") + 1);
                        parsedLine = parsedLine.Substring(0, parsedLine.IndexOf("</p>")) + " ";
                        content += parsedLine;
                    }
                }
                file.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message.ToString());
            }

            return content;
        }
    }
}
