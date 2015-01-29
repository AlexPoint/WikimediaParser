using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using WikitionaryParser.Src.Phrases;
using WikitionaryParser.Src.Proverbs;

namespace WikitionaryParser.Src
{
    public class WikitionaryParser
    {
        public const string WikitionaryRootUrl = "http://en.wiktionary.org";

        public List<Proverb> ParseAllEnglishProverbs()
        {
            var results = new List<Proverb>();
            var parser = new EnglishProverbParser();

            var currentPageUrl = "/wiki/Category:English_proverbs";
            while (!string.IsNullOrEmpty(currentPageUrl))
            {
                Console.WriteLine("Parsing proverbs in '{0}", currentPageUrl);

                // parse phrase in this page
                var urls = parser.ParseProverbPageUrlsIn(currentPageUrl);
                foreach (var url in urls)
                {
                    try
                    {
                        var proverb = parser.ParseProverbPage(url);
                        results.Add(proverb);
                    }
                    catch (Exception)
                    {
                        // fails silently
                        Console.WriteLine("Couldn't parse proverb at '{0}'", url);
                    }
                }

                // go to next page (if any)
                currentPageUrl = parser.ParseNextPageUrl(currentPageUrl);
            }

            return results;
        }

        public List<Phrase> ParseAllEnglishPhrases()
        {
            var results = new List<Phrase>();
            var parser = new EnglishPhrasesParser();
            
            var currentPageUrl = "/wiki/Category:English_phrases";
            while (!string.IsNullOrEmpty(currentPageUrl))
            {
                Console.WriteLine("Parsing phrases in '{0}", currentPageUrl);

                // parse phrase in this page
                var urls = parser.ParsePhrasePageUrlsIn(currentPageUrl);
                var phrases = urls.Select(u => parser.ParsePhrasePage(u));
                results.AddRange(phrases);

                // go to next page (if any)
                currentPageUrl = parser.ParseNextPageUrl(currentPageUrl);
            }

            return results;
        }
    }
}
