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
            {3, "Compute word frequencies"},
            {4, "Post process frequencies"},
            {5, "Compute bigram frequencies"},
            {6, "Post process bigram frequencies"}
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
                        break;
                    case 2:
                        ExtractTextFromDumpFiles();
                        break;
                    case 3:
                        BuildFrequencyDictionary();
                        break;
                    case 4:
                        PostProcessFrequencyDictionary();
                        break;
                    case 5:
                        BuildNgramsFrequencies();
                        break;
                    case 6:
                        PostProcessBiGramsFrequencies();
                        break;
                    default:
                        Console.WriteLine("This action is not supported");
                        break;
                }
            }

            
            Console.ReadKey();
        }

        private static readonly string PathToDownloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wikimedia\\Downloads\\";

        private static void PostProcessBiGramsFrequencies()
        {
            Console.WriteLine("Filter collocations with frequency less than:");
            var collocationFrequencyFilter = int.Parse(Console.ReadLine());

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Start post processing ngrams frequencies");

            // Load results
            var result = new CollocationResults();

            var collocationDirectory = PathToDownloadDirectory + "collocation";
            if (!Directory.Exists(collocationDirectory))
            {
                Directory.CreateDirectory(collocationDirectory);
            }
            var wordFrequencyFilePath = collocationDirectory + "/word-frequencies.txt";
            var ngramFreqFilePath = collocationDirectory + "/ngrams-frequencies.txt";
            result.LoadResults(wordFrequencyFilePath, ngramFreqFilePath);

            // Save frequency files on disk
            var ngramsPmisFilePath = collocationDirectory + "/ngrams-pmis.txt";
            result.SaveCollocationPMIs(ngramsPmisFilePath, collocationFrequencyFilter);

            Console.WriteLine("Done post processing ngrams frequencies");
            sw.Stop();
            Console.WriteLine("Executed in {0}", sw.Elapsed.ToString("g"));
        }

        private static void BuildNgramsFrequencies()
        {
            var result = new CollocationResults();

            Console.WriteLine("How many sentences do you want to parse?");
            var nbOfSentencesToParse = int.Parse(Console.ReadLine());

            Console.WriteLine("Flush the ngrams with frequency below:");
            var flushMinFrequency = int.Parse(Console.ReadLine());
            Console.WriteLine("Flush ngrams with low frequency every x sentence. x?");
            var flushNbOfSentences = int.Parse(Console.ReadLine());
            
            var nbOfAlreadyParsedSentences = 0;
            var collocationDirectory = PathToDownloadDirectory + "collocation";
            if (!Directory.Exists(collocationDirectory))
            {
                Directory.CreateDirectory(collocationDirectory);
            }
            var wordFrequencyFilePath = collocationDirectory + "/word-frequencies.txt";
            var ngramFreqFilePath = collocationDirectory + "/ngrams-frequencies.txt";
            var nbOfSentencesParsedFilePath = collocationDirectory + "/nbOfSentencesParsed.txt";
            var parsingResumed = false;
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
                        parsingResumed = true;
                    }
                }
            }

            // Final frequency list
            Console.WriteLine("Load frequency list");
            var frequencyDirectory = PathToDownloadDirectory + "frequencies";
            var frequencyListPath = frequencyDirectory + "/frequency-list - 150m.txt";
            var freqDic = new Dictionary<string, long>();
            using (var reader = new StreamReader(File.OpenRead(frequencyListPath)))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        freqDic.Add(parts[0], long.Parse(parts[1]));
                    }

                    line = reader.ReadLine();
                }
            }

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Building of collocation PMIs started");

            // Tokenize the sentences and compute the frequencies
            Func<string[], int, bool> extractTokens = (tokens, sentenceCounter) =>
            {
                if (sentenceCounter%flushNbOfSentences == 0)
                {
                    var nbOfFlushedNGrams = result.FlushNgramsWithFrequencyBelow(flushMinFrequency);
                    Console.WriteLine("Flushed {0} ngrams with frequency below {1}", nbOfFlushedNGrams, flushMinFrequency);
                }

                // Lowercase the first token if necessary
                if (tokens.Length > 0 && !string.IsNullOrEmpty(tokens[0]) && char.IsLetter(tokens[0][0]))
                {
                    long freq;
                    long lcFreq;
                    if (freqDic.TryGetValue(tokens[0], out freq) && freqDic.TryGetValue(LowerCaseFirstLetter(tokens[0]), out lcFreq) && lcFreq > freq)
                    {
                        tokens[0] = LowerCaseFirstLetter(tokens[0]);
                    }
                }
                result.AddBigrams(tokens);
                return true;
            };
            ExtractTokensFromTxtFiles(extractTokens, nbOfSentencesToParse, nbOfAlreadyParsedSentences);

            // Final flushing
            Console.WriteLine("Flushed {0} ngrams with frequency below {1}", result.FlushNgramsWithFrequencyBelow(flushMinFrequency), flushMinFrequency);

            // Load previous frequency dictionaries that were already computed
            Console.WriteLine("Loading previous results");
            if (parsingResumed)
            {
                result.LoadResults(wordFrequencyFilePath, ngramFreqFilePath);
            }

            // Save results on disk for later
            Console.WriteLine("Saving results on disk");
            result.SaveResults(wordFrequencyFilePath, ngramFreqFilePath);
            // Save the nb of sentences parsed (for information and being able to relaunch the parsing at this point)
            File.WriteAllText(nbOfSentencesParsedFilePath, nbOfSentencesToParse.ToString());

            Console.WriteLine("Building of collocation PMIs done");
            Console.WriteLine("=====================================");

            sw.Stop();
            Console.WriteLine("Ellapsed time: {0}", sw.Elapsed.ToString("g"));
        }

        private static void PostProcessFrequencyDictionary()
        {
            Console.WriteLine("Which minimum frequency should be used for building dictionaries?");
            var minFrequency = int.Parse(Console.ReadLine());

            Console.WriteLine("Started writing frequency list");

            var result = new FrequencyResults();
            var frequencyDirectory = PathToDownloadDirectory + "frequencies";
            var frequencyFilePath = frequencyDirectory + "/frequencies.txt";

            result.LoadFrequencyDictionary(frequencyFilePath, minFrequency);

            var groupedTokens = new Dictionary<string, List<WordOccurrenceAndFrequency>>();
            foreach (var wordFrequency in result.WordFrequencies)
            {
                var lcToken = wordFrequency.Key.Word.ToLowerInvariant();
                if (groupedTokens.ContainsKey(lcToken))
                {
                    groupedTokens[lcToken].Add(new WordOccurrenceAndFrequency()
                    {
                        Word = wordFrequency.Key.Word,
                        IsFirstTokenInSentence = wordFrequency.Key.IsFirstTokenInSentence,
                        Frequency = wordFrequency.Value
                    });
                }
                else
                {
                    groupedTokens[lcToken] = new List<WordOccurrenceAndFrequency>()
                    {
                        new WordOccurrenceAndFrequency()
                        {
                            Word = wordFrequency.Key.Word,
                            IsFirstTokenInSentence = wordFrequency.Key.IsFirstTokenInSentence,
                            Frequency = wordFrequency.Value
                        }
                    };
                }
            }

            var mergedTokens = new Dictionary<string, List<WordOccurrenceAndFrequency>>();
            foreach (var grp in groupedTokens)
            {
                var list = grp.Value.Where(v => !v.IsFirstTokenInSentence).ToList();

                // Merge tokens which are first in sentence with others
                foreach (var token in grp.Value.Where(wf => wf.IsFirstTokenInSentence))
                {
                    // Find other tokens which are not the first in the sentence and increase their frequency respectively to their frequency
                    var otherTokens = list.Where(v => v.Word == token.Word || v.Word == LowerCaseFirstLetter(token.Word)).ToList();
                    if (otherTokens.Any())
                    {
                        var totalGrpFreq = otherTokens.Sum(t => t.Frequency);
                        foreach (var otherToken in otherTokens)
                        {
                            otherToken.Frequency += (token.Frequency*otherToken.Frequency)/totalGrpFreq;
                        }

                    }
                    else
                    {
                        list.Add(token);
                    }
                }

                mergedTokens.Add(grp.Key, list);
            }

            // Post processed frequencies for debug
            var postProcessedFrequencyFilePath = frequencyDirectory + "/post-processed-frequencies.txt";
            var ppLines = mergedTokens
                .Select(ent => string.Join("|||",
                            ent.Value.Select(wf => string.Format("{0}|{1}|{2}", wf.Word, wf.IsFirstTokenInSentence, wf.Frequency))));
            File.WriteAllLines(postProcessedFrequencyFilePath, ppLines);

            // Final frequency list
            var frequencyListPath = frequencyDirectory + "/frequency-list.txt";
            var flLines = mergedTokens
                .SelectMany(ent => ent.Value)
                .OrderByDescending(wf => wf.Frequency)
                .Select(wf => string.Format("{0}|{1}", wf.Word, wf.Frequency));
            File.WriteAllLines(frequencyListPath, flLines);

            Console.WriteLine("Finished writing ferquency list");
        }

        private static string LowerCaseFirstLetter(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return "";
            }
            return char.ToLower(word[0]) + word.Substring(1);
        }

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

        private static void ExtractTokensFromTxtFiles(Func<string[], int, bool> tokensProcessor,  int nbOfSentencesToParse,
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
                        var success = tokensProcessor(tokens, sentenceCounter);
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
            var frequencyFilePath = frequencyDirectory + "/frequencies.txt";
            var excludedFrequencyFilePath = frequencyDirectory + "/excluded-frequencies.txt";
            var nbOfSentencesParsedFilePath = frequencyDirectory + "/nbOfSentencesParsed.txt";
            var parsingResumed = false;
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
                        parsingResumed = true;
                    }
                }
            }

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Building of frequency dictionary started");

            // Tokenize the sentences and compute the frequencies
            Func<string[], int, bool> extractTokens = (tokens, sentenceCounter) =>
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

            // Load previous frequency dictionaries that were already computed
            if (parsingResumed)
            {
                result.LoadFrequencyDictionary(frequencyFilePath);
                result.LoadFrequencyDictionary(excludedFrequencyFilePath);
            }
            
            // Save frequency files on disk
            result.SaveFrequencyDictionary(frequencyFilePath);
            result.SaveExcludedFrequencyDictionary(excludedFrequencyFilePath);

            // Save the nb of sentences parsed (for information and being able to relaunch the parsing at this point)
            File.WriteAllText(nbOfSentencesParsedFilePath, nbOfSentencesToParse.ToString());

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
