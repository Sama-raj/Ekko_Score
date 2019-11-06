using System;
using Fclp;

namespace AzureSearchCrawler
{
    /// <summary>
    /// The entry point of the crawler. Adjust the constants at the top and run.
    /// </summary>
    class CrawlerMain
    {
        private const int DefaultMaxPagesToIndex = 100;

        private class Arguments
        {
            public string RootUri { get; set; }

            public int MaxPagesToIndex { get; set; }

            public string ServiceName { get; set; }

            public string IndexName { get; set; }

            public string AdminApiKey { get; set; }
        }

        static void Main(string[] args)
        {
            var p = new FluentCommandLineParser<Arguments>();
            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\g\Downloads\" + args[0]);
            //string[] lines = System.IO.File.ReadAllLines(@"C:\Users\g\Downloads\myurls816.txt");

            // Display the file contents by using a foreach loop.
            foreach (string line in lines)
            {
                // Use a tab to indent each line of the file.
                System.Console.WriteLine("Contents of text file = " + line);



                Arguments arguments = new Arguments();
                //arguments.RootUri = line;
                arguments.RootUri = line.Replace('"', ' ').Replace(',', ' ').Trim();
                arguments.ServiceName = "localhost";
                arguments.IndexName = "IndexName";
                //arguments.AdminApiKey = "ed83fde2a56041ab83d75fe2437adb19";
                arguments.AdminApiKey = "1ccabb26e2134039ba6680434bd1a3ed";
                //ed83fde2a56041ab83d75fe2437adb19

                var indexer = new AzureSearchIndexer(arguments.ServiceName, arguments.IndexName, arguments.AdminApiKey, new TextExtractor());
                var crawler = new Crawler(indexer);
                crawler.Crawl(arguments.RootUri, maxPages: 20).Wait();
            }
           // Console.Read(); // keep console open until a button is pressed so we see the output
        }
    }
}
