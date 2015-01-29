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
            var definitionsAndExamples = new List<DefinitionAndExamples>();
            var defNodes = document.SelectNodes("//div[@id='mw-content-text']/ol[1]/li");
            foreach (var defNode in defNodes)
            {
                var clone = defNode.CloneNode(true);
                var childrenToRemove = clone.SelectNodes("./dl|./ul");
                if (childrenToRemove != null)
                {
                    foreach (var childToRemove in childrenToRemove)
                    {
                        clone.RemoveChild(childToRemove);
                    }
                }
                var definition = clone.InnerText.Trim();

                var examples = defNode.SelectNodes("./dl/dd") != null
                    ? defNode.SelectNodes("./dl/dd").Select(exNode => HtmlEntity.DeEntitize(exNode.InnerText.Trim())).ToList()
                    : new List<string>();
                var quotes = defNode.SelectNodes("./ul/li//dd") != null
                    ? defNode.SelectNodes("./ul/li//dd").Select(ddNode => HtmlEntity.DeEntitize(ddNode.InnerText.Trim())).ToList()
                    : new List<string>();

                definitionsAndExamples.Add(new DefinitionAndExamples()
                {
                    Definition = definition,
                    Examples = examples,
                    Quotes = quotes
                });
            }

            // synonyms
            var synonyms = new List<string>();
            var syonoymNodes = document.SelectNodes("//span[@id='Synonyms']/../following-sibling::ul//a");
            if (syonoymNodes != null)
            {
                synonyms = syonoymNodes
                    .Select(a => HtmlEntity.DeEntitize(a.InnerText.Trim()))
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
