using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using WikitionaryParser.Src.Phrases;

namespace WikitionaryParser.Src.Idioms
{
    /// <summary>
    /// A wikitionary parser specific to English idioms
    /// </summary>
    public class EnglishIdiomsParser
    {
        private readonly HtmlWeb _web = new HtmlWeb();

        private List<string> H3HeadLinesToIgnore = new List<string>()
        {
            "Alternative forms", "See also", "Etymology", "References", "External links", "Pronunciation",
            "Usage notes", "Related terms", "Anagrams", "Synonyms", "Quotations", "Derived terms"
        };

        public Idiom ParseIdiomPage(string relativeUrl)
        {
            var absoluteUrl = WikitionaryParser.WikitionaryRootUrl + relativeUrl;
            var document = _web.Load(absoluteUrl).DocumentNode;

            // delete all nodes for other sections than English
            var nodesToRemove = document.SelectNodes("//hr/following-sibling::*");
            if (nodesToRemove != null)
            {
                foreach (var nodeToRemove in nodesToRemove)
                {
                    nodeToRemove.Remove();
                }
            }

            // name
            var name = HtmlEntity.DeEntitize(document.SelectSingleNode("//h1[@id='firstHeading']").InnerText.Trim());

            // usages
            var usages = new List<Usage>();
            var relevantUsageSections = document.SelectNodes("//h3/span[@class='mw-headline']");
            if (relevantUsageSections != null)
            {
                foreach (var relevantUsageSection in relevantUsageSections.Where(s => !H3HeadLinesToIgnore.Contains(s.InnerText.Trim())))
                {
                    var olNode = relevantUsageSection.SelectSingleNode("./../following-sibling::ol");
                    var definitionsAndExamples = new List<DefinitionAndExamples>();
                    var defNodes = olNode.SelectNodes("./li");
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

                    var usage = new Usage()
                    {
                        DefinitionsAndExamples = definitionsAndExamples,
                        PartOfSpeech = HtmlEntity.DeEntitize(relevantUsageSection.InnerText.Trim())
                    };
                    usages.Add(usage);
                }
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
            
            // Categories
            var categories = document.SelectNodes("//div[@id='mw-normal-catlinks']/ul/li/a")
                .Select(n => n.InnerText.Trim())
                .ToList();

            return new Idiom()
            {
                Name = name,
                Synonyms = synonyms,
                SourceRelativeUrl = relativeUrl,
                Categories = categories,
                Usages = usages
            };
        }

        public List<string> ParseIdiomPageUrlsIn(string relativeUrl)
        {
            var absoluteUrl = WikitionaryParser.WikitionaryRootUrl + relativeUrl;
            var document = _web.Load(absoluteUrl).DocumentNode;

            var phrasePageUrls = document
                .SelectNodes("//div[@id='mw-pages']//table//li//a")
                .Where(a => a.HasAttributes && a.Attributes.Contains("href"))
                .Select(a => a.Attributes["href"].Value)
                .ToList();

            return phrasePageUrls;
        }

        public string ParseNextPageUrl(string relativeUrl)
        {
            var absoluteUrl = WikitionaryParser.WikitionaryRootUrl + relativeUrl;
            var document = _web.Load(absoluteUrl).DocumentNode;

            var nextPageNode = document.SelectNodes("//div[@id='mw-pages']/a")
                .FirstOrDefault(n => n.HasAttributes && n.Attributes.Contains("href") && n.InnerText == "next 200");
            if (nextPageNode != null)
            {
                return HtmlEntity.DeEntitize(nextPageNode.Attributes["href"].Value);
            }
            else
            {
                return null;
            }
        }
    }
}
