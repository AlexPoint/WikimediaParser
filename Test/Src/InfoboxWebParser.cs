using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Test.Src
{
    public class InfoboxWebParser
    {
        private string WhatsLinkHereUrl { get; set; }


        public InfoboxWebParser(string whatsLinkHereUrl)
        {
            WhatsLinkHereUrl = whatsLinkHereUrl;
        }


        public List<string> GetArticlesUrl()
        {
            var urls = ExtractArticleUrls(WhatsLinkHereUrl);

            var nextPageUrl = ExtractNextPageUrl(WhatsLinkHereUrl);
            while (!string.IsNullOrEmpty(nextPageUrl))
            {
                var otherUrls = ExtractArticleUrls(nextPageUrl);
                if (otherUrls.All(ou => urls.Contains(ou)))
                {
                    break;
                }
                urls.AddRange(otherUrls);
                nextPageUrl = ExtractNextPageUrl(nextPageUrl);
            }

            return urls;
        }

        private List<string> ExtractArticleUrls(string url)
        {
            var web = new HtmlWeb();
            var doc = web.Load(url);

            var urls = doc.DocumentNode
                .SelectNodes("//ul[@id='mw-whatlinkshere-list']/li/a")
                .SelectMany(node => node.Attributes)
                .Where(a => a.Name == "href")
                .Select(a => a.Value)
                .ToList();

            return urls;
        }

        private string ExtractNextPageUrl(string url)
        {
            var web = new HtmlWeb();
            var doc = web.Load(url);

            var nextPageUrl = doc.DocumentNode
                .Descendants()
                .Where(n => n.Name == "a" & n.HasAttributes & n.Attributes.Any(att => att.Name == "href") & n.InnerText == "next 500")
                .SelectMany(n => n.Attributes)
                .Where(att => att.Name == "href")
                .Select(att => att.Value)
                .FirstOrDefault();

            return !string.IsNullOrEmpty(nextPageUrl) ? "https://en.wikipedia.org" + HttpUtility.HtmlDecode(nextPageUrl) : null;
        }


        public List<ParsedInfobox> GetArticleInfobox(string articleUrl)
        {
            var fullUrl = "https://en.wikipedia.org" + articleUrl;

            var web = new HtmlWeb();
            var doc = web.Load(fullUrl);

            var infoboxes = doc.DocumentNode
                .Descendants()
                .Where(n => n.Name == "table" & n.HasAttributes & n.Attributes.Any(att => att.Value.Contains("infobox")))
                .Select(n => new ParsedInfobox()
                {
                    ArticleUrl = articleUrl,
                    Html = n.OuterHtml,
                    Properties = n.Descendants().Where(d => d.Name == "tr").Select(np => new InfoboxProperty()
                    {
                        Property = np.Descendants().Where(desc => desc.Name == "th").Select(th => th.InnerText).FirstOrDefault(),
                        Value = np.Descendants().Where(desc => desc.Name == "td").Select(td => td.InnerText).FirstOrDefault()
                    })
                    .Where(prop => !string.IsNullOrEmpty(prop.Property) & !string.IsNullOrEmpty(prop.Value))
                    .ToList()
                })
                .ToList();
            return infoboxes;
        }
    }
}
