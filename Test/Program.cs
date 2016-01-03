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
        private static readonly Regex LetterOnlyRegex = new Regex(@"^[a-zA-Z]+$", RegexOptions.Compiled);

        static void Main(string[] args)
        {
            // Compare an article's raw and cleaned content
            //CompareWikiTextAndCleanText("Aristotle");

            // ------------------------

            // There are 5,046,277 articles on English wikipedia
            const long flushBatchSize = 100000;
            const long nbOfSentencesToParse = 1000000;
            var pathToFlushWordsFileFormat = PathToProject + "Data/flush/flushed-words.{0}.txt";
            var pathToOccurenceNotFoundFileFormat = PathToProject + "Data/results/not-found-occurrences.{0}.txt";
            var pathToMergedOccurrenceFileFormat = PathToProject + "Data/results/merged-occurrences.{0}.txt";
            var wordFrequencies = new Dictionary<string, long>();
            
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
                if (sentenceCounter%flushBatchSize == 0 && sentenceCounter > 0)
                {
                    Console.WriteLine("Step #{0}; page '{1}'", sentenceCounter/flushBatchSize, sentenceAndWikiPage.Item2.Title);

                    var batchIndex = sentenceCounter/flushBatchSize;
                    // Post process word frequencies
                    var mergeFilePath = string.Format(pathToMergedOccurrenceFileFormat, batchIndex);
                    var notFoundFilePath = string.Format(pathToOccurenceNotFoundFileFormat, batchIndex);
                    var batchWordFrequencies = FrequencyResults.Instance.BuildFrequencyDictionary(mergeFilePath, notFoundFilePath);
                    // Merge with existing dictionary (keep only words 
                    foreach (var wordFrequency in batchWordFrequencies.Where(ent => ent.Value > 1 || LetterOnlyRegex.IsMatch(ent.Key.Word)))
                    {
                        if (wordFrequencies.ContainsKey(wordFrequency.Key.Word))
                        {
                            // Update the frequency
                            wordFrequencies[wordFrequency.Key.Word] += wordFrequency.Value;
                        }
                        else
                        {
                            // Add the entry
                            wordFrequencies.Add(wordFrequency.Key.Word, wordFrequency.Value);
                        }
                    }
                }

                if (string.IsNullOrEmpty(sentenceAndWikiPage.Item1))
                {
                    Console.WriteLine("Empty article (redirect?): " + sentenceAndWikiPage.Item2.Title);
                }
                // Tokenize sentences and add the tokens with their frequency in the FrequencyResults object
                var wordsAndFrequencies = tokenizer.Tokenize(sentenceAndWikiPage.Item1)
                    .Where(token => !string.IsNullOrEmpty(token))
                    .Select((token, index) => new
                    {
                        Word = token.Trim(),
                        IsFirstTokenOfSentence = index == 0
                    })
                    .GroupBy(a => a)
                    .Select(grp => new Tuple<WordOccurrence, long>(new WordOccurrence()
                    {
                        Word = grp.Key.Word,
                        IsFirstTokenInSentence = grp.Key.IsFirstTokenOfSentence
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
            var lines = wordFrequencies
                .OrderByDescending(wf => wf.Value)
                .Select(ent => string.Format("{0}|{1}", ent.Key, ent.Value));
            File.WriteAllLines(pathToFrequencyFile, lines);
            
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
