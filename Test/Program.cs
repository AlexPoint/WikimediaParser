using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WikitionaryDumpParser.Src;
using WikitionaryParser.Src.Frequencies;
using WikitionaryParser.Src.Idioms;
using WikitionaryParser.Src.Phrases;

namespace Test
{
    class Program
    {
        private static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";
        private static readonly string PathToSerializedIdioms = PathToProject + "Data/idioms.nbin";
        private static readonly string PathToWiktionaryPages = PathToProject + "Data/enwiktionary-20150901-pages-meta-current.xml";

        //private static readonly List<string> EnglishStopWords = new List<string>() { "a", "about", "above", "after", "again", "against", "all", "am", "an", "and", "any", "are", "aren't", "as", "at", "be", "because", "been", "before", "being", "below", "between", "both", "but", "by", "can't", "cannot", "could", "couldn't", "did", "didn't", "do", "does", "doesn't", "doing", "don't", "down", "during", "each", "few", "for", "from", "further", "had", "hadn't", "has", "hasn't", "have", "haven't", "having", "he", "he'd", "he'll", "he's", "her", "here", "here's", "hers", "herself", "him", "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", "i've", "if", "in", "into", "is", "isn't", "it", "it's", "its", "itself", "let's", "me", "more", "most", "mustn't", "my", "myself", "no", "nor", "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our", "ours", "ourselves", "out", "over", "own", "same", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", "so", "some", "such", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then", "there", "there's", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those", "through", "to", "too", "under", "until", "up", "very", "was", "wasn't", "we", "we'd", "we'll", "we're", "we've", "were", "weren't", "what", "what's", "when", "when's", "where", "where's", "which", "while", "who", "who's", "whom", "why", "why's", "with", "won't", "would", "wouldn't", "you", "you'd", "you'll", "you're", "you've", "your", "yours", "yourself", "yourselves" };

        static void Main(string[] args)
        {
            // Parse and retrieve page ids 
            var pagePropsSqlDumFile = PathToProject + "Data/enwiki-20150901-page_props.sql";
            var pageIdsFilePath = PathToProject + "Output/pageIds.txt";
            var parser = new WikiMediaMySqlDumpParser();
            //parser.ParsePageIds(pagePropsSqlDumFile, pageIdsFilePath);

            var pageIds = File.ReadLines(pageIdsFilePath)
                .Where(line => !string.IsNullOrEmpty(line) && line.Contains("|"))
                .Select(line => new Tuple<int, string>(int.Parse(line.Split('|').First()), line.Split('|').Last()))
                .ToList();
            Console.WriteLine("{0} page ids found", pageIds.Count());

            // Parse and retrieve fr language links
            var langLinksSqlDumFile = PathToProject + "Data/enwiki-20150901-langlinks.sql";
            var langLinksFilePath = PathToProject + "Output/langLinks.txt";
            //parser.ParseLanguageLinks(langLinksSqlDumFile, langLinksFilePath);

            var languageLinks = File.ReadLines(langLinksFilePath)
                .Where(line => !string.IsNullOrEmpty(line) && line.Contains("|"))
                .Select(line => new Tuple<int, string, string>(int.Parse(line.Split('|').First()), line.Split('|')[1], line.Split('|').Last()))
                .ToList();
            Console.WriteLine("{0} language links found", pageIds.Count());

            // Show results
            Console.WriteLine("--- Sample of parsed translations ---");
            foreach (var languageLink in languageLinks)
            {
                // Filter non-relevant pages such as 
                // - categories: George Kearsley Shaw
                // - proper nouns: Mingus, Charles
                if (IsEntryRelevantForTranslation(languageLink.Item3))
                {
                    var linkedPage = pageIds.FirstOrDefault(pid => pid.Item1 == languageLink.Item1);
                    if (linkedPage != null)
                    {
                        Console.WriteLine("{0} -> {1}", linkedPage.Item2, languageLink.Item3);
                    }
                    else
                    {
                        Console.WriteLine("Missing page id #{0} (lang link '{1}')", languageLink.Item1, languageLink.Item3);
                    }
                }
            }

            // Parse dictionary in wiktionary page content
            /*var srcLanguage = "en";
            var tgtLanguages = new List<string>() {"fr"};
            var outputFilePath = PathToProject + "Output/en-fr-dictionary.txt";

            var dumpParser = new WiktionaryDumpParser.Src.WiktionaryDumpParser();
            var entries = dumpParser.ParseDumpFile(PathToWiktionaryPages, srcLanguage, tgtLanguages, outputFilePath);
            
            var posTranslations = entries
                .GroupBy(ent => ent.Pos)
                .Select(grp => new PosTranslations()
                {
                    Pos = grp.Key,
                    SynsetTranslations = grp.GroupBy(ent => ent.Synset)
                        .Select(synGrp => new SynsetTranslation()
                        {
                            Definition = synGrp.Key,
                            Translations = synGrp.Select(ent => new Translation()
                            {
                                Language = ent.Language,
                                Name = ent.Name
                            })
                            .ToList()
                        })
                        .ToList()
                })
                .ToList();
            Console.WriteLine("Parsed {0} {1} entries ({2} distinct)", entries.Count, srcLanguage, entries.Select(ent => ent.Name).Distinct().Count());*/

            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }

        private static Regex ProperNounRegex = new Regex(@"[A-Z][a-z]+( [A-Z][a-z]+)+", RegexOptions.Compiled);
        private static Regex CategoryRegex = new Regex("Catégorie:.*", RegexOptions.Compiled);
        private static bool IsEntryRelevantForTranslation(string frName)
        {
            if (string.IsNullOrEmpty(frName) 
                || ProperNounRegex.IsMatch(frName) 
                || CategoryRegex.IsMatch(frName))
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
