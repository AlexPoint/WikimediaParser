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

        static void Main(string[] args)
        {
            var outputFile = PathToProject + "Output\\en-fr-dictionary.txt";

            var dumpDownloader = new DumpDownloader();

            // Download files
            var enPagePropsFilePath = dumpDownloader.DownloadFile("enwiki-latest-page.sql.gz");
            var enLangLinksFilePath = dumpDownloader.DownloadFile("enwiki-latest-langlinks.sql.gz");

            // Parse language links
            Console.WriteLine("Start parsing language links");
            var parser = new MySqlDumpParser();
            var languageLinks = parser.ParseLanguageLinks(enLangLinksFilePath)
                .ToDictionary(ll => ll.PageId, ll => ll);
            Console.WriteLine("{0} language links found", languageLinks.Count());

            // Show results
            Console.WriteLine("Start associating pages and language links");
            var counter = 0;
            var translatedEntities = new List<TranslatedEntity>();
            var pageInfoReader = new DumpFileReader(enPagePropsFilePath);
            var pageInfo = pageInfoReader.ReadNext();
            while (pageInfo != null)
            {
                LanguageLink languageLink;
                if (languageLinks.TryGetValue(pageInfo.Id, out languageLink))
                {
                    counter++;
                    translatedEntities.Add(new TranslatedEntity()
                    {
                        //SrcLanguage = "en",
                        SrcName = pageInfo.GetDisplayedTitle(),
                        //TgtLanguage = languageLink.LanguageCode,
                        TgtName = languageLink.Title
                    });
                }

                pageInfo = pageInfoReader.ReadNext();
            }
            Console.WriteLine("Found {0} translations", counter);

            // Write all translated entities
            File.AppendAllLines(outputFile, translatedEntities.Select(te => te.ToString()));

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
