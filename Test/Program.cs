using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using OpenNLP.Tools.SentenceDetect;
using OpenNLP.Tools.Tokenize;
using Test.Src;
using WikitionaryDumpParser.Src;

namespace Test
{
    class Program
    {
        private static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";
        private static readonly Regex LetterOnlyRegex = new Regex(@"^[a-zA-Z]+$", RegexOptions.Compiled);

        private static readonly Dictionary<int, string> Actions = new Dictionary<int, string>()
        {
            {1, "Download dump file"},
            {2, "Extract text from dump files"},
            {3, "Compute word frequencies"}
        };

        static void Main(string[] args)
        {
            //CompareWikiTextAndCleanText("Algorithms_(journal)");

            Console.WriteLine("Which action do you want to do?");
            foreach (var action in Actions)
            {
                Console.WriteLine("{0} -> press {1}", action.Value, action.Key);
            }
            var result = Console.ReadLine();
            Console.WriteLine();

            int actionNumber;
            if (int.TryParse(result, out actionNumber))
            {
                switch (actionNumber)
                {
                    case 1:
                        DownloadDumpFiles();
                        return;
                    case 2:
                        ExtractTextFromDumpFiles();
                        return;
                    case 3:
                        return;
                }
            }

            Console.WriteLine("This action is not supported");
            Console.ReadKey();

            // Compare an article's raw and cleaned content
            //CompareWikiTextAndCleanText("Aristotle");

            // ------------------------

            // There are 5,046,277 articles on English wikipedia as of Dec, 2015
            const long flushBatchSize = 1000000;
            const long nbOfSentencesToParse = 500000000;
            var pathToFlushWordsFileFormat = PathToProject + "Data/flush/flushed-words.{0}.txt";
            var pathToOccurenceNotFoundFileFormat = PathToProject + "Data/results/not-found-occurrences.{0}.txt";
            var pathToMergedOccurrenceFileFormat = PathToProject + "Data/results/merged-occurrences.{0}.txt";
            var wordFrequencies = new Dictionary<string, long>();
            
            // Downloads the dump file with the latest wikipedia pages' content.
            var dumpDownloader = new DumpDownloader(PathToDownloadDirectory);
            var pageDumpFileName = string.Format("{0}{1}-latest-pages-meta-current.xml.bz2", "en", "wiki");
            var dumpFilePaths = dumpDownloader.DownloadLatestFiles("wiki", "en");

            // Create the NLP objects necessary for processing the articles' text content (sentence detector and tokenizer)
            var sentenceDetector = new EnglishMaximumEntropySentenceDetector(PathToProject + "Data/EnglishSD.nbin");
            var tokenizer = new EnglishRuleBasedTokenizer(false);
            var wikimarkupCleaner = new WikiMarkupCleaner();
            var sentenceReader = new WikimediaSentencesReader(dumpFilePaths, title => title.Contains(":"),
                wikimarkupCleaner, sentenceDetector);

            // Browse et process all pages
            Console.WriteLine("Start parsing {0} sentence in dump file", nbOfSentencesToParse);
            var stopWatch = Stopwatch.StartNew();
            var sentenceCounter = 0;
            var sentenceAndWikiPage = sentenceReader.ReadNext();
            while (sentenceAndWikiPage != null && sentenceCounter < nbOfSentencesToParse)
            {
                if (sentenceCounter % flushBatchSize == 0 && sentenceCounter > 0)
                {
                    Console.WriteLine("Step #{0}; page '{1}' (#{2})", sentenceCounter/flushBatchSize, sentenceAndWikiPage.Item2.Title, sentenceReader.WikiPageCounter);

                    var batchIndex = sentenceCounter/flushBatchSize;
                    
                    // Post process word frequencies
                    var mergeFilePath = string.Format(pathToMergedOccurrenceFileFormat, batchIndex);
                    var notFoundFilePath = string.Format(pathToOccurenceNotFoundFileFormat, batchIndex);
                    var batchWordFrequencies = FrequencyResults.Instance.BuildFrequencyDictionary(mergeFilePath, notFoundFilePath);

                    // Merge with existing dictionary (keep only words)
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

            /*// Force flushing of the words still in the dictionary
            // Post process word frequencies
            var lastMergeFilePath = string.Format(pathToMergedOccurrenceFileFormat, "last");
            var lastNotFoundFilePath = string.Format(pathToOccurenceNotFoundFileFormat, "last");
            var batchWordFrequencies = FrequencyResults.Instance.BuildFrequencyDictionary(lastMergeFilePath, lastNotFoundFilePath);

            // Merge with existing dictionary (keep only words)
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
            }*/

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

        private static readonly string PathToDownloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wikimedia\\Downloads\\";


        private static string SanitizeFileName(string fileName)
        {
            var sb = new StringBuilder();
            foreach (var c in fileName)
            {
                sb.Append(Path.GetInvalidFileNameChars().Contains(c) ? '_' : c);
            }
            return sb.ToString();
        }

        private static void ExtractTextFromDumpFiles()
        {
            Console.WriteLine("Extraction of text from dump files started");
            var wikiMarkupCleaner = new WikiMarkupCleaner();

            var relevantFilePaths = Directory.GetFiles(PathToDownloadDirectory)
                .Where(f => Regex.IsMatch(f, "enwiki-latest-pages-meta-current\\d") && Path.GetExtension(f) == ".bz2")
                .ToList();
            Predicate<string> pageFilterer = s => s.Contains(":");
            foreach (var relevantFilePath in relevantFilePaths)
            {
                Console.WriteLine("Start extracting text from {0}", relevantFilePath);

                var fileName = Path.GetFileNameWithoutExtension(relevantFilePath);

                // We extract the articles in the directory with the same name
                var directoryPath = PathToDownloadDirectory + fileName;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var xmlReader = new XmlDumpFileReader(relevantFilePath);
                var next = xmlReader.ReadNext(pageFilterer);
                while (next != null)
                {
                    var filePath = directoryPath + "/" + SanitizeFileName(next.Title) + ".txt";
                    // Cleanup article content
                    var cleanedText = wikiMarkupCleaner.CleanArticleContent(next.Text);

                    if (!string.IsNullOrEmpty(cleanedText))
                    {
                        File.WriteAllText(filePath, cleanedText); 
                    }

                    next = xmlReader.ReadNext(pageFilterer);
                }

                Console.WriteLine("Done extraction text from {0}", relevantFilePath);
                Console.WriteLine("{0} articles extracted", Directory.GetFiles(directoryPath).Count());
            }
            Console.WriteLine("Extraction of text from dump files done");
            Console.WriteLine("========================================");
        }
        
        private static void DownloadDumpFiles()
        {
            Console.WriteLine("Downloading of dump files started");

            Console.WriteLine("Downloads are stored in directory '{0}'", PathToDownloadDirectory);
            var dumpDownloader = new DumpDownloader(PathToDownloadDirectory);
            var dumpFilePaths = dumpDownloader.DownloadLatestFiles("wiki", "en");
            Console.WriteLine("Downloaded files:");
            foreach (var dumpFilePath in dumpFilePaths)
            {
                Console.WriteLine("- {0}", dumpFilePath);
            }

            Console.WriteLine("Downloading of dump files done");
            Console.WriteLine("===============================");
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
