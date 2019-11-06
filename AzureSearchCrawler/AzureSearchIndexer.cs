using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Abot.Poco;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using ClosedXML.Excel;

namespace AzureSearchCrawler
{
    /// <summary>
    /// A CrawlHandler that indexes crawled pages into Azure Search. Pages are represented by the nested WebPage class.
    /// <para/>To customize what text is extracted and indexed from each page, you implement a custom TextExtractor
    /// and pass it in.
    /// </summary>
    class AzureSearchIndexer : CrawlHandler
    {
        private const int IndexingBatchSize = 25;

        private TextExtractor _textExtractor;
        private ISearchIndexClient _indexClient;

        private BlockingCollection<WebPage> _queue = new BlockingCollection<WebPage>();
        private SemaphoreSlim indexingLock = new SemaphoreSlim(1, 1);

        public AzureSearchIndexer(string serviceName, string indexName, string adminApiKey, TextExtractor textExtractor)
        {
            _textExtractor = textExtractor;

            SearchServiceClient serviceClient = new SearchServiceClient(serviceName, new SearchCredentials(adminApiKey));
            _indexClient = serviceClient.Indexes.GetClient(indexName);
        }

        public async Task PageCrawledAsync(CrawledPage crawledPage)
        {
            string text = _textExtractor.ExtractText(crawledPage.HtmlDocument);
            if (text == null)
            {
                Console.WriteLine("No content for page {0}", crawledPage?.Uri.AbsoluteUri);
                return;
            }

            _queue.Add(new WebPage(crawledPage.Uri.AbsoluteUri, text));

            if (_queue.Count > IndexingBatchSize - 1)
            {
                await IndexBatchIfNecessary();
            }
        }

        public async Task CrawlFinishedAsync()
        {
            await IndexBatchIfNecessary();

            // sanity check
            if (_queue.Count > 0)
            {
                Console.WriteLine("Error: indexing queue is still not empty at the end.");
            }
        }

        private async Task<DocumentIndexResult> IndexBatchIfNecessary()
        {
            await indexingLock.WaitAsync();

            if (_queue.Count == 0)
            {
                return null;
            }

            int batchSize = Math.Min(_queue.Count, IndexingBatchSize);
            Console.WriteLine("Indexing batch of {0}", batchSize);

            try
            {
                var pages = new List<WebPage>(batchSize);
                for (int i = 0; i < batchSize; i++)
                {
                    pages.Add(_queue.Take());
                }
                if (pages.Count > 0)
                {
                    InsertToExcel(pages, 1);
                }
                var batch = IndexBatch.MergeOrUpload(pages);
                return await _indexClient.Documents.IndexAsync(batch);
            }
            finally
            {
                indexingLock.Release();
            }
        }

        public void InsertToExcel(List<WebPage> records, int i)
        {
            var workbook = new XLWorkbook((@"D:\rmit\project\data\Webcrawler.xlsx"));
            try
            {
                //workbook.AddWorksheet("sheetName");
                if (workbook.Worksheets.Count == 0) { workbook.AddWorksheet("sheetName"); }
                var ws = workbook.Worksheet("sheetName");
                int NumberOfLastRow = 0;
                if (ws.LastRowUsed() != null)
                {
                    NumberOfLastRow = ws.LastRowUsed().RowNumber();
                }
                //IXLCell CellForNewData = ws.Cell(NumberOfLastRow + 1, 1);

                int row = NumberOfLastRow + 1;
                foreach (var item in records)
                {
                    ws.Cell("A" + row.ToString()).Value = item.Url.ToString();
                    ws.Cell("B" + row.ToString()).Value = item.Content.ToString();
                    row++;
                }

                workbook.SaveAs(@"D:\rmit\project\data.xlsx");
            }
            catch (Exception e)
            {
                workbook.SaveAs(@"D:\rmit\project\data\Webcrawler.xlsx");
            }
        }

        public void InsertDataToDb(List<WebPage> records)
        {
            string connectionString = "Data Source=.;Initial Catalog=DatabaseName;Integrated Security=true";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd =
                    new SqlCommand(
                        "INSERT INTO TableName (param1, param2, param3) VALUES (@param1, @param2, @param3)");
                cmd.CommandType = CommandType.Text;
                cmd.Connection = conn;
                foreach (var item in records)
                {
                    cmd.Parameters.AddWithValue("@param1", item.Id);
                    cmd.Parameters.AddWithValue("@param2", item.Url);
                    cmd.Parameters.AddWithValue("@param3", item.Content);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    conn.Close();
                }
            }
        }

        [SerializePropertyNamesAsCamelCase]
        public class WebPage
        {
            public WebPage(string url, string content)
            {
                Url = url;
                Content = content;
                Id = url.GetHashCode().ToString();
            }

            public string Id { get; }

            public string Url { get; }

            public string Content { get; }
        }
    }
}