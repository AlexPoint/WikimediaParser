using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;

namespace WikitionaryDumpParser.Src
{
    /// <summary>
    /// Creates a translation dictionary from WikiMedia resources (wikipedia, wiktionary...)
    /// </summary>
    public class DictionaryBuilder
    {
        private static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";

        /// <summary>
        /// Creates a translation dictionary between <paramref name="srcLanguage"/> and <paramref name="tgtLanguage"/> by:
        /// - downloading the appropriate files
        /// - extracting information from them
        /// - associating the info of the different files
        /// </summary>
        /// <param name="srcLanguage">The ISO-639-1 code for the source language</param>
        /// <param name="tgtLanguage">The ISO-639-1 code for the target language</param>
        /// <param name="wikimedia">The wikipedia resource (wikipedia, witionary...)</param>
        /// <returns>The collection of translated entities</returns>
        public string CreateDictionary(string srcLanguage, string tgtLanguage, Wikimedia wikimedia)
        {
            Console.WriteLine("Start creating dictionary {0}-{1}", srcLanguage, tgtLanguage);

            // Creates the output file name
            var outputFilePath = PathToProject + string.Format("Output\\{0}-{1}-{2}-dictionary.txt", 
                srcLanguage, tgtLanguage, Enum.GetName(typeof(Wikimedia), wikimedia));

            TranslatedEntities translatedEntities;
            if (wikimedia == Wikimedia.Wikipedia)
            {
                // Creates the dictionary from the language links between the pages
                translatedEntities = ExtractTranslatedEntitiesFromLanguageLinks(srcLanguage, tgtLanguage, wikimedia);
            }
            else if (wikimedia == Wikimedia.Wiktionary)
            {
                // Creates the dictionary from the content of wiktionary pages (a translation section is sometimes present on wiktionary)
                translatedEntities = ExtractTranslatedEntitiesFromWiktionaryContent(srcLanguage, tgtLanguage, wikimedia);
            }
            else
            {
                throw new ArgumentException(string.Format("Wikimedia '{0}' is not supported for building translation dictionaries", Enum.GetName(typeof(Wikimedia), wikimedia)));
            }

            // Write all translated entities
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
            File.AppendAllLines(outputFilePath, translatedEntities.GetTextFileLines());

            Console.WriteLine("Finished creating dictionary {0}-{1}", srcLanguage, tgtLanguage);
            Console.WriteLine("----");
            return outputFilePath;
        }
        
        /// <summary>
        /// Creates a translation dictionary from languagelinks for a wikimedia resource. Language links are a specific kind of
        /// interwikilinks (ie links between two different domains - en.wikipedia.org and fr.wikipedia.org for instance).
        /// See https://en.wiktionary.org/wiki/Help:FAQ#What_are_interwiki_links.3F for more details.
        /// </summary>
        /// <param name="srcLanguage">The ISO-639-1 code for the source language</param>
        /// <param name="tgtLanguage">The ISO-639-1 code for the target language</param>
        /// <param name="wikimedia">The wikipedia resource (wikipedia, witionary...)</param>
        /// <returns>The collection of translated entities</returns>
        private TranslatedEntities ExtractTranslatedEntitiesFromLanguageLinks(string srcLanguage, string tgtLanguage, Wikimedia wikimedia)
        {
            var translatedEntities = new TranslatedEntities()
            {
                SrcLanguage = srcLanguage,
                TgtLanguage = tgtLanguage,
                Entities = new List<TranslatedEntity>()
            };

            // Download dump files with pages and langlinks of the source language (always take the latest version)
            var dumpDownloader = new DumpDownloader();
            var pageDumpFileName = string.Format("{0}{1}-latest-page.sql.gz", srcLanguage, GetWikimediaExtension(wikimedia));
            var srcPagePropsFilePath = dumpDownloader.DownloadFile(pageDumpFileName);
            var langLinksDumpFileName = string.Format("{0}{1}-latest-langlinks.sql.gz", srcLanguage, GetWikimediaExtension(wikimedia));
            var srcLangLinksFilePath = dumpDownloader.DownloadFile(langLinksDumpFileName);

            // Parse language links and load them in dictionary for fast retrieval
            Console.WriteLine("Start parsing language links");
            var parser = new DumpParser();
            var languageLinks = parser.ParseLanguageLinks(srcLangLinksFilePath, tgtLanguage)
                .ToDictionary(ll => ll.Id, ll => ll);
            Console.WriteLine("{0} language links found", languageLinks.Count());

            // Associate the pages (with title in src language) with the language links (with title in tgt language)
            Console.WriteLine("Start associating pages and language links");
            var counter = 0;
            var pageInfoReader = new DumpFileReader(srcPagePropsFilePath);
            var pageInfo = pageInfoReader.ReadNext();
            while (pageInfo != null)
            {
                LanguageLink languageLink;
                if (languageLinks.TryGetValue(pageInfo.Id, out languageLink))
                {
                    counter++;
                    translatedEntities.Entities.Add(new TranslatedEntity()
                    {
                        SrcName = pageInfo.GetDisplayedTitle(),
                        TgtName = languageLink.GetDisplayedTitle()
                    });
                }

                pageInfo = pageInfoReader.ReadNext();
            }
            Console.WriteLine("Associated {0} entries for {1}-{2}", counter, srcLanguage, tgtLanguage);

            return translatedEntities;
        }

        private TranslatedEntities ExtractTranslatedEntitiesFromWiktionaryContent(string srcLanguage, string tgtLanguage,
            Wikimedia wikimedia)
        {
            // Download dump files with pages and langlinks of the source language (always take the latest version)
            var dumpDownloader = new DumpDownloader();
            var pagesDumpFileName = string.Format("{0}{1}-latest-pages-meta-current.xml.bz2", srcLanguage, GetWikimediaExtension(wikimedia));
            var srcPageFilePath = dumpDownloader.DownloadFile(pagesDumpFileName);

            var parser = new WiktionaryDumpParser.Src.WiktionaryDumpParser();
            var translatedEntities = parser.ExtractTranslatedEntities(srcPageFilePath, srcLanguage, tgtLanguage);

            return translatedEntities;
        }

        private string GetWikimediaExtension(Wikimedia wikimedia)
        {
            switch (wikimedia)
            {
                case Wikimedia.Wikipedia:
                    return "wiki";
                case Wikimedia.Wiktionary:
                    return "wiktionary";
                default:
                    throw new ArgumentException("Wikimedia '{0}' is not supported", Enum.GetName(typeof(Wikimedia), wikimedia));
            }
        }
    }

    public enum Wikimedia
    {
        Wikipedia, Wiktionary
    }
}
