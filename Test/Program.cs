using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using ICSharpCode.SharpZipLib.BZip2;
using OpenNLP.Tools.SentenceDetect;
using OpenNLP.Tools.Tokenize;
using Test.Src;
using WikitionaryDumpParser.Src;
using WikitionaryParser.Src.Idioms;

namespace Test
{
    class Program
    {
        private static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";

        static void Main(string[] args)
        {
            // Compare an article's raw and cleaned content
            CompareWikiTextAndCleanText("Abraham_Lincoln");

            // ------------------------

            const int nbOfSentencesToParse = 100000;
            
            // Downloads the dump file with the latest wikipedia pages' content.
            var dumpDownloader = new DumpDownloader();
            var pageDumpFileName = string.Format("{0}{1}-latest-pages-meta-current.xml.bz2", "en", "wiki");
            var dumpFilePath = dumpDownloader.DownloadFile(pageDumpFileName);

            // Create the NLP objects necessary for processing the articles' text content (sentence detector and tokenizer)
            var sentenceDetector = new EnglishMaximumEntropySentenceDetector(PathToProject + "Data/EnglishSD.nbin");
            var tokenizer = new EnglishRuleBasedTokenizer();
            var wikimarkupCleaner = new WikiMarkupCleaner();
            var sentenceReader = new WikimediaSentencesReader(dumpFilePath, title => title.Contains(":"),
                wikimarkupCleaner, sentenceDetector);

            // Browse et process all pages
            Console.WriteLine("Start parsing {0} sentence in dump file", nbOfSentencesToParse);
            var stopWatch = Stopwatch.StartNew();
            var sentenceCounter = 0;
            var sentenceAndWikiPage = sentenceReader.ReadNext();
            while (sentenceAndWikiPage != null && sentenceCounter < nbOfSentencesToParse)
            {
                // Tokenize sentences and add the tokens with their frequency in the FrequencyResults object
                var wordsAndFrequencies = tokenizer.Tokenize(sentenceAndWikiPage.Item1)
                    .Where(token => !string.IsNullOrEmpty(token))
                    .Select((token, index) => new
                    {
                        Word = token.Trim(),
                        IsFirstTokenOfSentence = index == 0
                    })
                    .GroupBy(a => a)
                    .Select(grp => new Tuple<WordAndFrequency, long>(new WordAndFrequency()
                    {
                        Word = grp.Key.Word,
                        IsFirstLineToken = grp.Key.IsFirstTokenOfSentence
                    }, grp.Count()))
                    .ToList();
                FrequencyResults.Instance.AddOccurrences(wordsAndFrequencies, sentenceAndWikiPage.Item2.Title);

                sentenceAndWikiPage = sentenceReader.ReadNext();
                sentenceCounter++;
            }
            
            stopWatch.Stop();
            Console.WriteLine("Parsed {0} sentences in {1}", sentenceCounter, stopWatch.Elapsed.ToString("g"));

            // Write frequency results
            Console.WriteLine("Writing frequencies");
            var pathToFrequencyFile = PathToProject + "Data/frequency-results.txt";
            var pathToExcludedFrequencyFile = PathToProject + "Data/excluded-frequency-results.txt";
            FrequencyResults.Instance.WriteFiles(pathToFrequencyFile, pathToExcludedFrequencyFile);
            
            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }

        /// <summary>
        /// Downloads the content of a wikipedia article, cleans it and persists both
        /// the raw and cleaned version of the article in the Data folder.
        /// </summary>
        private static void CompareWikiTextAndCleanText(string title)
        {
            var page = XmlDumpFileReader.GetPage(title);
            var wikiMarkupCleaner = new WikiMarkupCleaner();

            var pathToDirectory = PathToProject + "Data/CleanTextCompare/";
            if (!Directory.Exists(pathToDirectory))
            {
                Directory.CreateDirectory(pathToDirectory);
            }

            // Write the raw content of the page
            var rawFilePath = pathToDirectory + "raw.txt";
            File.WriteAllText(rawFilePath, page.Text);

            // Write the cleaned content of the page
            var cleanedText = wikiMarkupCleaner.CleanArticleContent(page.Text);
            var cleanedFilePath = pathToDirectory + "cleaned.txt";
            File.WriteAllText(cleanedFilePath, cleanedText);

            Console.WriteLine("Files with '{0}' page content (raw & cleaned) has been written", title);
        }

    }
}
