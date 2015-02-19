using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WikitionaryParser.Src.Frequencies
{
    public class WordFrequencyParser
    {
        private readonly HtmlWeb _web = new HtmlWeb();

        private readonly static List<string> GutembergProjectFrequencyListUrls = new List<string>()
        {
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/PG/2006/04/1-10000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/PG/2006/04/10001-20000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/PG/2006/04/20001-30000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/PG/2006/04/30001-40000"
        };

        private readonly static List<string> TvShowsFrequencyListUrls = new List<string>()
        {
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/1-1000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/1001-2000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/2001-3000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/3001-4000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/4001-5000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/5001-6000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/6001-7000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/7001-8000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/8001-9000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/9001-10000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/10001-12000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/12001-14000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/14001-16000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/16001-18000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/18001-20000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/20001-22000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/22001-24000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/24001-26000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/26001-28000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/28001-30000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/30001-32000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/32001-34000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/34001-36000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/36001-38000",
            "http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists/TV/2006/38001-40000"
        };

        public enum FrequencyListType
        {
            Gutemberg, TvShows
        }


        public List<WordAndFrequency> ParseWordFrequencies(FrequencyListType frequencyListType)
        {
            var wordFrequencies = new List<WordAndFrequency>();

            var pagesUrls = frequencyListType == FrequencyListType.Gutemberg
                ? GutembergProjectFrequencyListUrls
                : TvShowsFrequencyListUrls;
            foreach (var url in pagesUrls)
            {
                var doc = _web.Load(url).DocumentNode;
                var frequenciesInPage = doc.SelectNodes("//div[@id='mw-content-text']/table//tr")
                    .Where(tr => tr.SelectNodes("./td[position()=2]/a") != null)
                    .Select(n => new WordAndFrequency()
                    {
                        Word = n.SelectNodes("./td[position()=2]/a").First().InnerText,
                        Frequency = float.Parse(n.SelectNodes("./td[position()=3]").First().InnerText.Split(' ').First(), CultureInfo.InvariantCulture)
                    });
                wordFrequencies.AddRange(frequenciesInPage);
            }

            return wordFrequencies;
        }
    }
}
