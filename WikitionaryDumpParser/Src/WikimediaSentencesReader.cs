using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNLP.Tools.SentenceDetect;
using Test.Src;

namespace WikitionaryDumpParser.Src
{
    public class WikimediaSentencesReader
    {
        private readonly Predicate<string> pageFilterer;
        private readonly ISentenceDetector sentenceDetector;
        private readonly XmlDumpFileReader xmlDumpFileReader;
        private readonly WikiMarkupCleaner wikiMarkupCleaner;
        private WikiPage currentWikiPage;
        private readonly List<string> stackedSentences = new List<string>();

        public WikimediaSentencesReader(string localDumpFilePath, Predicate<string> pageFilterer, WikiMarkupCleaner wikiMarkupCleaner,
            ISentenceDetector sentenceDetector)
        {
            this.xmlDumpFileReader = new XmlDumpFileReader(localDumpFilePath);
            this.pageFilterer = pageFilterer;
            this.sentenceDetector = sentenceDetector;
            this.wikiMarkupCleaner = wikiMarkupCleaner;
        }

        /// <summary>
        /// Reads the next sentence (and the wiki page where it has been parsed) in the wikimedia dump file
        /// </summary>
        /// <returns></returns>
        public Tuple<string, WikiPage> ReadNext()
        {
            if (currentWikiPage == null || !this.stackedSentences.Any())
            {
                currentWikiPage = xmlDumpFileReader.ReadNext();
                while (currentWikiPage == null || pageFilterer(currentWikiPage.Title))
                {
                    currentWikiPage = xmlDumpFileReader.ReadNext();
                }

                if (currentWikiPage == null)
                {
                    // We arrived at the end of the dump file
                    return null;
                }
                else
                {
                    // Cleanup article content
                    var cleanedText = this.wikiMarkupCleaner.CleanArticleContent(currentWikiPage.Text);

                    // Split in sentences
                    var cleanedSentences = cleanedText
                        .Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                        .SelectMany(line => sentenceDetector.SentenceDetect(line))
                        .ToList();

                    if (cleanedSentences.Any())
                    {
                        this.stackedSentences.AddRange(cleanedSentences);
                    }
                    else
                    {
                        // Reads the next page
                        return this.ReadNext();
                    }
                }

            }
            
            // Just pop the next sentence
            var nextSentence = this.stackedSentences[0];
            this.stackedSentences.RemoveAt(0);
            return new Tuple<string, WikiPage>(nextSentence, currentWikiPage);
        }
    }
}
