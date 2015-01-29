using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikitionaryParser.Src.Phrases;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new EnglishPhrasesParser();
            var urls = parser.ParsePhrasePageUrlsIn("http://en.wiktionary.org/wiki/Category:English_phrases");

            foreach (var url in urls)
            {
                var absoluteUrl = WikitionaryParser.Src.WikitionaryParser.WikitionaryRootUrl + url;
                var phrase = parser.ParsePhrasePage(absoluteUrl);
                phrase.Print();
            }

            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }
    }
}
