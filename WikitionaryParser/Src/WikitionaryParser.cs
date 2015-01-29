using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using WikitionaryParser.Src.Phrases;

namespace WikitionaryParser.Src
{
    public class WikitionaryParser
    {
        public const string WikitionaryRootUrl = "http://en.wiktionary.org";
        private const string WikiRootUrl = WikitionaryRootUrl + "/wiki/";


        public List<Phrase> ParseAllEnglishPhrases()
        {
            var englishPhrasesRootUrl = WikitionaryRootUrl + "Category:English_phrases";

            var web = new HtmlWeb();
            var document = web.Load(englishPhrasesRootUrl);

            return null;
        }
    }
}
