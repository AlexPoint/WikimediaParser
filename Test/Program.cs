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
using OpenNLP.Tools.Tokenize;
using Test.Src;
using WikitionaryDumpParser.Src;
using WikitionaryParser.Src.Idioms;

namespace Test
{
    class Program
    {
        private static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";
        private static readonly string PathToSerializedIdioms = PathToProject + "Data/idioms.nbin";
        private static readonly string PathToWiktionaryPages = PathToProject + "Data/enwiktionary-20150901-pages-meta-current.xml";

        static void Main(string[] args)
        {
            var nbOfPagesToParse = 10000;
            
            var dumpDownloader = new DumpDownloader();
            var pageDumpFileName = string.Format("{0}{1}-latest-pages-meta-current.xml.bz2", "en", "wiki");
            var dumpFilePath = dumpDownloader.DownloadFile(pageDumpFileName);

            //var sentenceDetector = new OpenNLP.Tools.SentenceDetect.EnglishMaximumEntropySentenceDetector("");
            var tokenizer = new EnglishRuleBasedTokenizer();

            var stopWatch = new Stopwatch();
            Console.WriteLine("Parsing wikitext");
            stopWatch.Start();
            var xmlDumpFileReader = new XmlDumpFileReader(dumpFilePath);
            WikiPage page = xmlDumpFileReader.ReadNext();
            var pageCounter = 0;
            while (page != null && pageCounter < nbOfPagesToParse)
            {
                var cleanedTokens = WikiMarkupCleaner.CleanupFullArticle(page.Text)
                    .SelectMany(line => tokenizer.Tokenize(line))
                    .Where(token => !string.IsNullOrEmpty(token))
                    .Select(token => token.Trim());
                FrequencyResults.Instance.AddOccurrences(cleanedTokens, page.Title);

                pageCounter++;
                page = xmlDumpFileReader.ReadNext();
            }
            stopWatch.Stop();
            Console.WriteLine("Parsed {0} wiki pages in {1}", pageCounter, stopWatch.Elapsed.ToString("g"));

            // Write frequency results
            Console.WriteLine("Writing frequencies");
            var pathToFrequencyFile = PathToProject + "Data/frequency-results.txt";
            var pathToExcludedFrequencyFile = PathToProject + "Data/excluded-frequency-results.txt";
            FrequencyResults.Instance.WriteFiles(pathToFrequencyFile, pathToExcludedFrequencyFile);
            
            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }

        private static readonly Regex ProperNounRegex = new Regex(@"[A-Z][a-z]+( [A-Z][a-z]+)+", RegexOptions.Compiled);
        private static readonly Regex CategoryRegex = new Regex("Catégorie:.*", RegexOptions.Compiled);
        private static readonly Regex ContainsNumberRegex = new Regex(@"\d{2,}", RegexOptions.Compiled);
        private static bool IsEntryRelevantForTranslation(string frName)
        {
            if (string.IsNullOrEmpty(frName) 
                || ProperNounRegex.IsMatch(frName) 
                || CategoryRegex.IsMatch(frName)
                || ContainsNumberRegex.IsMatch(frName))
            {
                return false;
            }

            return true;
        }

        private static void SerializeIdioms(List<Idiom> idioms, string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Create))
            {
                var bin = new BinaryFormatter();
                bin.Serialize(stream, idioms);
            }
        }

        private static List<Idiom> DeserializeIdioms(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var bin = new BinaryFormatter();
                var idioms = (List<Idiom>)bin.Deserialize(stream);
                return idioms;
            }
        }
    }
}
