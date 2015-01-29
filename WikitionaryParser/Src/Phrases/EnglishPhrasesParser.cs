using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WikitionaryParser.Src.Phrases
{
    public class EnglishPhrasesParser
    {

        public Phrase ParsePhrasePage(string url)
        {
            var web = new HtmlWeb();
            var document = web.Load(url).DocumentNode;

            // name
            var name = HtmlEntity.DeEntitize(document.SelectSingleNode("//h1[@id='firstHeading']").InnerText.Trim());

            // definitions and examples
            var definitionsAndExamples = document.SelectNodes("//div[@id='mw-content-text']/ol/li")
                .Select(n => new DefinitionAndExamples()
                {
                    Definition =
                        string.Join(" ",
                            n.ChildNodes.Where(child => child.Name != "dl").Select(child => child.InnerText)),
                    Examples = n.SelectNodes(".//dl/dd") != null ?
                        n.SelectNodes(".//dl/dd").Select(exNode => exNode.InnerText.Trim()).ToList():
                        new List<string>()
                })
                .ToList();

            // synonyms
            var synonyms = new List<string>();
            var syonoymNodes = document.SelectNodes("//span[@id='Synonyms']/parent/following-sibling//a");
            if (syonoymNodes != null)
            {
                synonyms = syonoymNodes
                    .Select(a => a.InnerText.Trim())
                    .ToList();
            }

            return new Phrase()
            {
                Name = name,
                DefinitionsAndExamples = definitionsAndExamples,
                Synonyms = synonyms
            };
        }

        public List<string> ParsePhrasePageUrlsIn(string url)
        {
            var web = new HtmlWeb();
            var document = web.Load(url).DocumentNode;

            var phrasePageUrls = document
                .SelectNodes("//div[@id='mw-pages']//table//li//a")
                .Where(a => a.HasAttributes && a.Attributes.Contains("href"))
                .Select(a => a.Attributes["href"].Value)
                .ToList();

            return phrasePageUrls;
        }

        public string ParseNextPageUrl(string url)
        {
            var web = new HtmlWeb();
            var document = web.Load(url).DocumentNode;

            var nextPageNode = document.SelectNodes("//div[@id='mw-pages']/a")
                .FirstOrDefault(n => n.HasAttributes && n.Attributes.Contains("href") && n.InnerText == "next 200");
            if(nextPageNode != null)
            {
                return nextPageNode.Attributes["href"].Value;
            }
            else
            {
                return null;
            }
        }
    }
}
