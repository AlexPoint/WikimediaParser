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
                        DownloadEnglishWikipediaDumpFiles();
                        return;
                    case 2:
                        ExtractTextFromDumpFiles();
                        return;
                    case 3:
                        BuildFrequencyDictionary();
                        Console.ReadKey();
                        return;
                }
            }

            Console.WriteLine("This action is not supported");
            Console.ReadKey();
        }

        private static readonly string PathToDownloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wikimedia\\Downloads\\";

        /// <summary>
        /// Replace all invalid filename characters by '_'
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            var sb = new StringBuilder();
            foreach (var c in fileName)
            {
                sb.Append(Path.GetInvalidFileNameChars().Contains(c) ? '_' : c);
            }
            return sb.ToString();
        }

        private static void ExtractTokensFromTxtFiles(Func<string[], bool> tokensProcessor, int nbOfSentencesToParse,
            int nbOfSentencesToSkip = 0)
        {
            var relevantDirectories = Directory.GetDirectories(PathToDownloadDirectory)
                .Where(dir => Regex.IsMatch(dir, "enwiki-latest-pages-meta-current"));
            var sentenceDetector = new EnglishMaximumEntropySentenceDetector(PathToProject + "Data/EnglishSD.nbin");
            var tokenizer = new EnglishRuleBasedTokenizer(false);

            var sentenceCounter = 0;
            foreach (var directory in relevantDirectories.OrderBy(d => d)) // ordering is important here to be able to relaunch the parsing from anywhere
            {
                var txtFiles = Directory.GetFiles(directory);
                foreach (var txtFile in txtFiles.OrderBy(f => f)) // ordering is important here to be able to relaunch the parsing from anywhere
                {
                    var sentences = File.ReadAllLines(txtFile)
                        .Where(l => !string.IsNullOrEmpty(l))
                        .SelectMany(l => sentenceDetector.SentenceDetect(l))
                        .ToList();
                    foreach (var sentence in sentences)
                    {
                        // Increase counter
                        sentenceCounter++;
                        if (sentenceCounter > nbOfSentencesToParse)
                        {
                            return;
                        }
                        if (sentenceCounter <= nbOfSentencesToSkip)
                        {
                            continue;
                        }

                        var tokens = tokenizer.Tokenize(sentence);
                        var success = tokensProcessor(tokens);
                    }
                }
                Console.WriteLine("Done parsing sentences in directory: '{0}'", directory);
            }
        }

        private static void BuildFrequencyDictionary()
        {
            var result = new FrequencyResults();

            Console.WriteLine("How many sentences do you want to parse?");
            var nbOfSentencesToParse = int.Parse(Console.ReadLine());

            var nbOfAlreadyParsedSentences = 0;
            var frequencyDirectory = PathToDownloadDirectory + "frequencies";
            if (!Directory.Exists(frequencyDirectory))
            {
                Directory.CreateDirectory(frequencyDirectory);
            }
            var nbOfSentencesParsedFilePath = frequencyDirectory + "/nbOfSentencesParsed.txt";
            if (File.Exists(nbOfSentencesParsedFilePath))
            {
                int nbOfSentencesParsed;
                if (int.TryParse(File.ReadAllText(nbOfSentencesParsedFilePath), out nbOfSentencesParsed))
                {
                    Console.WriteLine("{0} sentences have already been parsed. Resume parsing? (y/n)", nbOfSentencesParsed);
                    var resumeParsing = string.Equals(Console.ReadLine(), "Y", StringComparison.InvariantCultureIgnoreCase);
                    if (resumeParsing)
                    {
                        nbOfAlreadyParsedSentences = nbOfSentencesParsed; 

                    }
                }
            }

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Building of frequency dictionary started");

            // Tokenize the sentences and compute the frequencies
            Func<string[], bool> extractTokens = tokens =>
            {
                for (var i = 0; i < tokens.Length; i++)
                {
                    var wordOccurence = new WordOccurrence()
                    {
                        IsFirstTokenInSentence = i == 0,
                        Word = tokens[i]
                    };
                    result.AddOccurence(wordOccurence);
                }
                return true;
            };
            ExtractTokensFromTxtFiles(extractTokens, nbOfSentencesToParse, nbOfAlreadyParsedSentences);
            
            // Save frequency files on disk
            var frequencyFilePath = frequencyDirectory + "/frequencies.txt";
            result.SaveFrequencyDictionary(frequencyFilePath);
            var excludedFrequencyFilePath = frequencyDirectory + "/excluded-frequencies.txt";
            result.SaveExcludedFrequencyDictionary(excludedFrequencyFilePath);

            // Save the nb of sentences parsed (for information and being able to relaunch the parsing at this point)
            result.NbOfSentencesParsed = nbOfSentencesToParse;
            File.WriteAllText(nbOfSentencesParsedFilePath, result.NbOfSentencesParsed.ToString());

            Console.WriteLine("Building of frequency dictionary done");
            Console.WriteLine("=====================================");

            sw.Stop();
            Console.WriteLine("Ellapsed time: {0}", sw.Elapsed.ToString("g"));
        }

        /// <summary>
        /// For each dump files already downloaded on disk, extract the articles' text,
        /// clean it and save it in a specific text file.
        /// </summary>
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
                    try
                    {
                        var filePath = directoryPath + "/" + SanitizeFileName(next.Title) + ".txt";
                        // Cleanup article content
                        var cleanedText = wikiMarkupCleaner.CleanArticleContent(next.Text);

                        if (!string.IsNullOrEmpty(cleanedText))
                        {
                            File.WriteAllText(filePath, cleanedText);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception raised on article '{0}': {1}", next.Title, ex.Message);
                    }

                    next = xmlReader.ReadNext(pageFilterer);
                }

                Console.WriteLine("Done extraction text from {0}", relevantFilePath);
                Console.WriteLine("{0} articles extracted", Directory.GetFiles(directoryPath).Count());
                Console.WriteLine("--------");
            }
            Console.WriteLine("Extraction of text from dump files done");
            Console.WriteLine("========================================");
        }
        
        /// <summary>
        /// Download the English wikipedia dump files on disk.
        /// </summary>
        private static void DownloadEnglishWikipediaDumpFiles()
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
