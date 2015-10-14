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
using System.Xml;
using ICSharpCode.SharpZipLib.BZip2;
using Wiki;
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
            var dumpDownloader = new DumpDownloader();
            var pageDumpFileName = string.Format("{0}{1}-latest-pages-meta-current.xml.bz2", "en", "wiktionary");
            var dumpFilePath = dumpDownloader.DownloadFile(pageDumpFileName);

            var parser = new CreoleParser();
            using (var inputFileStream = File.OpenRead(dumpFilePath))
            {
                using (var decompressedStream = new BZip2InputStream(inputFileStream))
                {
                    var reader = XmlReader.Create(decompressedStream);

                    while (reader.ReadToFollowing("page"))
                    {
                        var foundTitle = reader.ReadToDescendant("title");
                        if (foundTitle)
                        {
                            var title = reader.ReadInnerXml();
                            var foundRevision = reader.ReadToNextSibling("revision");
                            if (foundRevision)
                            {
                                var foundText = reader.ReadToDescendant("text");
                                if (foundText)
                                {
                                    var text = reader.ReadInnerXml();
                                    
                                    var html = parser.ToHTML(text);
                                    var innerText = Regex.Replace(html, "<.*?>", string.Empty);
                                    
                                    Console.WriteLine(innerText);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Couldn't find title in node");
                        }
                    }
                }
            }

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
