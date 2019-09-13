using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using CsvHelper;
using OpenNLP.Tools.SentenceDetect;
using OpenNLP.Tools.Tokenize;
using Test.Src;
using Test.Src.DbContext;
using WikitionaryDumpParser.Src;
using WikitionaryDumpParser.Src.DbContext;

namespace Test
{
    class Program
    {
        
        private static readonly Regex LetterOnlyRegex = new Regex(@"^[a-zA-Z]+$", RegexOptions.Compiled);

        private static readonly Dictionary<int, string> Actions = new Dictionary<int, string>()
        {
            // Downloading dump files from wikipedia servers and extracting basic information
            {1, "Download dump file"},
            {2, "Extract text from dump files"},
            // Functions for computing ngrams and their frequencies
            {3, "Compute word frequencies"},
            {4, "Post process word frequencies"},
            {5, "Compute ngram frequencies"},
            {6, "Post process ngram frequencies"},
            {7, "Compute all ngrams frequencies"},
            // Steps for extracting infoboxes information from wikipedia html
            // -> html is generated from the wiki markdown so parsing directly from markdown is preferable
            {8, "Extract infobox properties"},
            {9, "Parse company infoboxes (web)"},
            // Steps for extracting infoboxes information from wiki dumps
            {20, "Parse company infoboxes (all English articles - from dumps 1/3)"},
            {21, "Process company infoboxes (from dumps 2/3)"},
            {22, "Write infobox properties to CSV (from dumps 3/3)"}
        };

        static void Main(string[] args)
        {
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
                        ComputeNgramsFrequencies();
                        break;
                    case 6:
                        PostProcessNgramsFrequencies();
                        break;
                    case 7:
                        ComputeAllNGramsFrequencies();
                        break;
                    case 8:
                        ParseInfoboxProperties();
                        break;
                    case 9:
                        WebParseCompanyInfoboxes();
                        break;
                    case 20:
                        ParseCompanyInfoboxesInDumps();
                        break;
                    case 21:
                        ProcessCompanyInfoboxes();
                        break;
                    case 22:
                        WriteInfoboxPropertiesToCsv();
                        break;
                    default:
                        Console.WriteLine("This action is not supported");
                        break;
                }
            }

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        /// <summary>
        /// Extracted from wikipedia dumps (step 3/3).
        /// Write the infobox properties to a CSV file for easy consumption.
        /// TODO: this step could be removed by moving the execution of the dump parsing directly in the ETL project.
        /// </summary>
        /// <param name="delimiter">The CSV delimiter to use when writing the CSV file</param>
        private static void WriteInfoboxPropertiesToCsv(string delimiter = "\t")
        {
            var applicationDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/../../";
            var fileName = string.Format("wiki-dumps-infoboxes-{0}.csv", "1"); // TODO: give a specific identifier to identify the parsing version
            var filePath = applicationDirectory + "Results/" + fileName;

            using (var db = new WikiContext())
            {
                using (var mem = new FileStream(filePath, FileMode.Create))
                using (var writer = new StreamWriter(mem))
                using (var csvWriter = new CsvWriter(writer))
                {
                    csvWriter.Configuration.Delimiter = delimiter;

                    csvWriter.WriteField("PageTitle");
                    csvWriter.WriteField("InfoboxId");
                    csvWriter.WriteField("Key");
                    csvWriter.WriteField("Value");
                    csvWriter.NextRecord();

                    foreach (var prop in db.RawInfoboxProperties)
                    {
                        csvWriter.WriteField(prop.PageTitle);
                        csvWriter.WriteField(prop.InfoboxId);
                        csvWriter.WriteField(prop.PropKey);
                        csvWriter.WriteField(prop.PropValue.Replace("\n", "/n")); // FIXME: hack to "escape" line returns in order not to screw up the CSV file
                        csvWriter.NextRecord();
                    }

                    /*writer.Flush();
                    var result = Encoding.UTF8.GetString(mem.ToArray());
                    Console.WriteLine(result);*/
                } 
            }
        }

        /// <summary>
        /// Extracted from wikipedia dumps (step 2/3).
        /// Process the markdown for each company infobox and extract the properties (key/value pairs).
        /// </summary>
        private static void ProcessCompanyInfoboxes()
        {
            var batchSize = 1000;
            var index = 0;
            using (var db = new WikiContext())
            {
                var infoboxes = db.RawDumpParsedInfoboxes.OrderBy(box => box.Id).Skip(index * batchSize).Take(batchSize).ToList();

                while (infoboxes.Any())
                {
                    if (index % 10 == 0)
                    {
                        Console.WriteLine("Start parsing batch #{0}", index);
                    }

                    foreach (var infobox in infoboxes)
                    {
                        var markdownRegex = new Regex(@"(\n\s*\||\|\s*\n)(?=[\sa-zA-Z_]+=)", RegexOptions.Compiled);
                        var parts = markdownRegex.Split(infobox.Markdown).ToList();
                        var properties = parts.Where(line => line.Contains("="))
                            .Select(line => line.Split(new char[] { '=' }, 2))
                            .Select(tup => new RawInfoboxProperty
                            {
                                PropKey = tup.First().Trim(),
                                PropValue = tup.Last().Trim(),
                                PageTitle = infobox.PageTitle,
                                InfoboxId = infobox.Id
                            })
                            .ToList();
                        db.RawInfoboxProperties.AddRange(properties);
                    }

                    db.SaveChanges();

                    index++;
                    infoboxes = db.RawDumpParsedInfoboxes.OrderBy(box => box.Id).Skip(index * batchSize).Take(batchSize).ToList();
                }
            }
            // One infobox doesn't have any property -> ATMNet (normal)

        }

        /// <summary>
        /// Extracted from wikipedia dumps (step 1/3).
        /// Browse the wiki dumps and extract the markdown for each company infobox.
        /// </summary>
        private static void ParseCompanyInfoboxesInDumps()
        {
            var generalStopwatch = Stopwatch.StartNew();

            var dumpDir = Utilities.PathToDownloadDirectory;
            foreach (var filePath in Directory.EnumerateFiles(dumpDir))
            {
                Console.WriteLine("Start parsing infoboxes in file {0}", Path.GetFileName(filePath));
                var stopwatch = Stopwatch.StartNew();

                var infoboxes = new List<RawDumpParsedInfobox>();

                var wikiReader = new XmlDumpFileReader(filePath);
                Predicate<string> pageFilterer = s => s.Contains(":"); // Real Wikipedia pages contains ":" (others are conversations etc.)
                var page = wikiReader.ReadNext(pageFilterer);
                while (page != null)
                {
                    var boxes = page.GetInfoboxTexts("company").Select(s => new RawDumpParsedInfobox()
                    {
                        Markdown = HttpUtility.HtmlDecode(s),
                        PageTitle = page.Title
                    });
                    infoboxes.AddRange(boxes);

                    page = wikiReader.ReadNext(pageFilterer);
                }

                stopwatch.Stop();
                Console.WriteLine("Parsed {0} infoboxes in {1}", infoboxes.Count, stopwatch.Elapsed.ToString());
                stopwatch.Restart();

                // Persist infoboxes
                using (var db = new WikiContext())
                {
                    db.RawDumpParsedInfoboxes.AddRange(infoboxes);
                    db.SaveChanges();
                }

                stopwatch.Stop();
                Console.WriteLine("Persisted {0} infoboxes in {1}", infoboxes.Count, stopwatch.Elapsed.ToString());
                Console.WriteLine("--");
            }


            generalStopwatch.Stop();
            Console.WriteLine("Total infobox parsing time: {0}", generalStopwatch.Elapsed.ToString());

        }


        private static void WebParseCompanyInfoboxes()
        {
            var stopwatch= Stopwatch.StartNew();

            var webParser = new InfoboxWebParser("https://en.wikipedia.org/w/index.php?title=Special:WhatLinksHere/Template:Infobox_company&limit=500");

            var urls = webParser.GetArticlesUrl();
            Console.WriteLine("Found {0} articles ({1})", urls.Count, stopwatch.Elapsed.ToString());

            var groups = urls.Select((u, i) => new { Url = u, Index = i }).GroupBy(a => a.Index / 1000).ToList();
            Console.WriteLine("Start parsing infoboxes for {0} groups of 1000 articles", groups.Count);
            foreach(var group in groups.Skip(69))
            {
                var boxes = new List<ParsedInfobox>();
                foreach(var url in group)
                {
                    boxes.AddRange(webParser.GetArticleInfobox(url.Url));
                }

                using(var db = new WikiContext())
                {
                    db.ParsedInfoboxes.AddRange(boxes);
                    db.SaveChanges();
                }

                Console.WriteLine("Group #{0} done", group.Key);
            }

            Console.Write("Parsed and persisted company infoboxes in {0}", stopwatch.Elapsed.ToString());
        }


        private static void ParseInfoboxProperties()
        {
            // We chose the strategy to do as much as possible directly in SQL, performance-wise.
            // However, we execute queries directly in SQL to limit the overhead of EntityFramework
            using(var db = new WikiContext())
            {
                // Queries can take a long time to run; set the timeout to 6 hours
                db.Database.CommandTimeout = 6 * 60 * 60;

                var stopwatch = Stopwatch.StartNew();

                // Update the Template Property of the parsed Infoboxes
                int nbInfoboxTemplatesUpdated = db.Database.ExecuteSqlCommand(@"
                    UPDATE Infoboxes
                    SET Template = TRIM(SUBSTRING(RawText, (CHARINDEX('{{Infobox ', RawText) + LEN('{{Infobox')), (CHARINDEX('|', RawText) - LEN('{{Infobox') - 1)))
                    WHERE RawText like '%{{Infobox%' and RawText like '%|%'");
                Console.WriteLine("{0} templates were extracted for infoboxes", nbInfoboxTemplatesUpdated);

                // Extract the infoboxes' properties in markup text and create the infoboxes' properties with them
                int nbOfInfoboxPropertiesCreated = db.Database.ExecuteSqlCommand(@"
                    INSERT INTO InfoboxProperties (Infobox_Id, RawText)
                    SELECT Id, value
                    FROM Infoboxes
	                    CROSS APPLY string_split(SUBSTRING(RawText, CHARINDEX('|', RawText) + 1, LEN(RawText)), '|')
                    WHERE RawText like '%{{Infobox%' and RawText like '%|%'
                    ORDER BY Infoboxes.Id");
                Console.WriteLine("{0} infobox properties were created", nbOfInfoboxPropertiesCreated);

                // Parse the infoboxes' properties' raw text
                int nbOfPropertiesUpdated = db.Database.ExecuteSqlCommand(@"
                    UPDATE InfoboxProperties
                    SET Key = TRIM(SUBSTRING(RawText, 0, CHARINDEX('='))),
	                    Value = TRIM(SUBSTRING(RawText, CHARINDEX('='), LEN(RawText))
                    WHERE RawText like '%=%'");
                Console.WriteLine("{0} infobox properties have been parsed", nbOfPropertiesUpdated);
            }

        }

        /*private static void ParseInfoboxes()
        {
            var generalStopwatch = Stopwatch.StartNew();

            var dumpDir = Utilities.PathToDownloadDirectory;
            foreach(var filePath in Directory.EnumerateFiles(dumpDir))
            {
                Console.WriteLine("Start parsing infoboxes in file {0}", Path.GetFileName(filePath));

                var stopwatch = Stopwatch.StartNew();

                var infoboxes = new List<Infobox>();

                var wikiReader = new XmlDumpFileReader(filePath);
                Predicate<string> pageFilterer = s => s.Contains(":"); // Real Wikipedia pages contains ":" (others are conversations etc.)
                var page = wikiReader.ReadNext(pageFilterer);
                while(page != null)
                {
                    var boxes = page.GetInfoboxTexts().Select(s => new Infobox()
                    {
                        RawText = s,
                        PageTitle = page.Title
                    });
                    infoboxes.AddRange(boxes);

                    page = wikiReader.ReadNext(pageFilterer);
                }

                stopwatch.Stop();
                Console.WriteLine("Parsed {0} infoboxes in {1}", infoboxes.Count, stopwatch.Elapsed.ToString());
                stopwatch.Restart();

                // Persist infoboxes
                using (var db = new WikiContext())
                {
                    db.Infoboxes.AddRange(infoboxes);
                    db.SaveChanges();
                }

                stopwatch.Stop();
                Console.WriteLine("Persisted {0} infoboxes in {1}", infoboxes.Count, stopwatch.Elapsed.ToString());
                Console.WriteLine("--");
            }


            generalStopwatch.Stop();
            Console.WriteLine("Total infobox parsing time: {0}", generalStopwatch.Elapsed.ToString());

        }*/

        private static void PostProcessNgramsFrequencies()
        {
            Console.WriteLine("For which value of 'n'?");
            var n = int.Parse(Console.ReadLine());

            Console.WriteLine("Filter collocations with frequency less than:");
            var collocationFrequencyFilter = int.Parse(Console.ReadLine());

            var builder = new NgramPmisBuilder(n, collocationFrequencyFilter);
            builder.ComputePmis();
        }

        private static void ComputeAllNGramsFrequencies()
        {
            Console.WriteLine("For which values of 'n'?");
            Console.WriteLine("Min value of n?");
            var min = int.Parse(Console.ReadLine());
            Console.WriteLine("Max value of n?");
            var max = int.Parse(Console.ReadLine());

            Console.WriteLine("How many sentences do you want to parse?");
            var nbOfSentencesToParse = int.Parse(Console.ReadLine());

            Console.WriteLine("Flush the ngrams with frequency below:");
            var flushMinFrequency = int.Parse(Console.ReadLine());
            Console.WriteLine("Flush ngrams with low frequency every x sentence. x?");
            var flushNbOfSentences = int.Parse(Console.ReadLine());

            for (var i = min; i <= max; i++)
            {
                Console.WriteLine("n = {0}", i);
                var ngramFreqBuilder = new NGramFrequencyBuilder(i, Utilities.PathToDownloadDirectory, nbOfSentencesToParse,
                flushMinFrequency, flushNbOfSentences);
                ngramFreqBuilder.ComputeNgramsFrequencies();

                Console.WriteLine("==============");
                Console.WriteLine();
            }
        }

        private static void ComputeNgramsFrequencies()
        {
            Console.WriteLine("For which value of 'n'?");
            var n = int.Parse(Console.ReadLine());

            Console.WriteLine("How many sentences do you want to parse?");
            var nbOfSentencesToParse = int.Parse(Console.ReadLine());

            Console.WriteLine("Flush the ngrams with frequency below:");
            var flushMinFrequency = int.Parse(Console.ReadLine());
            Console.WriteLine("Flush ngrams with low frequency every x sentence. x?");
            var flushNbOfSentences = int.Parse(Console.ReadLine());

            var ngramFreqBuilder = new NGramFrequencyBuilder(n, Utilities.PathToDownloadDirectory, nbOfSentencesToParse,
                flushMinFrequency, flushNbOfSentences);
            ngramFreqBuilder.ComputeNgramsFrequencies();
        }

        private static void PostProcessFrequencyDictionary()
        {
            Console.WriteLine("Which minimum frequency should be used for building dictionaries?");
            var minFrequency = int.Parse(Console.ReadLine());

            Console.WriteLine("Started writing frequency list");

            var result = new FrequencyResults();
            var frequencyDirectory = Utilities.PathToDownloadDirectory + "frequencies";
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
                    var otherTokens = list.Where(v => v.Word == token.Word || v.Word == Utilities.LowerCaseFirstLetter(token.Word)).ToList();
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
                .Select(ent => string.Join(Utilities.Csv2ndLevelSeparator,
                            ent.Value.Select(wf => string.Format("{0}{3}{1}{3}{2}", wf.Word, wf.IsFirstTokenInSentence, wf.Frequency, Utilities.CsvSeparator))));
            File.WriteAllLines(postProcessedFrequencyFilePath, ppLines);

            // Load fleex words
            var fleexWords = new HashSet<string>();
            var fleexWordsFile = frequencyDirectory + "/fleex - words.txt";
            foreach (var word in File.ReadAllLines(fleexWordsFile))
            {
                fleexWords.Add(word);
            }

            // Final frequency list
            var frequencyListPath = frequencyDirectory + "/frequency-list.txt";
            var flLines = mergedTokens
                .SelectMany(ent => ent.Value)
                .OrderByDescending(wf => wf.Frequency)
                .Select(wf => string.Format("{0},{1},{2},{3},{4}", wf.Word, wf.Frequency, 
                    char.IsUpper(wf.Word[0]) ? 1 : 0, wf.Word.Any(char.IsUpper) ? 1 : 0, 
                    fleexWords.Contains(wf.Word) ? 1 : 0));
            File.WriteAllLines(frequencyListPath, flLines);

            Console.WriteLine("Finished writing frequency list");
        }
        
        private static void BuildFrequencyDictionary()
        {
            var result = new FrequencyResults();

            Console.WriteLine("How many sentences do you want to parse?");
            var nbOfSentencesToParse = int.Parse(Console.ReadLine());

            var nbOfAlreadyParsedSentences = 0;
            var frequencyDirectory = Utilities.PathToDownloadDirectory + "frequencies";
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
            Utilities.ExtractTokensFromTxtFiles(extractTokens, nbOfSentencesToParse, nbOfAlreadyParsedSentences);

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

            var relevantFilePaths = Directory.GetFiles(Utilities.PathToDownloadDirectory)
                .Where(f => Regex.IsMatch(f, "enwiki-latest-pages-meta-current\\d") && Path.GetExtension(f) == ".bz2")
                .ToList();
            Predicate<string> pageFilterer = s => s.Contains(":");
            foreach (var relevantFilePath in relevantFilePaths)
            {
                Console.WriteLine("Start extracting text from {0}", relevantFilePath);

                var fileName = Path.GetFileNameWithoutExtension(relevantFilePath);

                // We extract the articles in the directory with the same name
                var directoryPath = Utilities.PathToDownloadDirectory + fileName;
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
                        var filePath = directoryPath + "/" + Utilities.SanitizeFileName(next.Title) + ".txt";
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

            Console.WriteLine("Downloads are stored in directory '{0}'", Utilities.PathToDownloadDirectory);
            var dumpDownloader = new DumpDownloader(Utilities.PathToDownloadDirectory);
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

            var pathToDirectory = Utilities.PathToProject + "Data/CleanTextCompare/";
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
