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
        private readonly List<XmlDumpFileReader> xmlDumpFileReaders;
        private int currentReaderIndex;
        private readonly WikiMarkupCleaner wikiMarkupCleaner;
        private WikiPage currentWikiPage;
        private readonly List<string> stackedSentences = new List<string>();

        public int WikiPageCounter { get; private set; }

        public WikimediaSentencesReader(List<string> localDumpFilePaths, Predicate<string> pageFilterer, WikiMarkupCleaner wikiMarkupCleaner,
            ISentenceDetector sentenceDetector)
        {
            this.xmlDumpFileReaders = localDumpFilePaths.Select(dp => new XmlDumpFileReader(dp)).ToList();
            currentReaderIndex = 0;
            this.pageFilterer = pageFilterer;
            this.sentenceDetector = sentenceDetector;
            this.wikiMarkupCleaner = wikiMarkupCleaner;
        }

        public Tuple<string, WikiPage> ReadNext()
        {
            if (currentReaderIndex >= xmlDumpFileReaders.Count)
            {
                return null;
            }

            var currentReader = xmlDumpFileReaders[currentReaderIndex];
            var next = ReadNextInReader(currentReader);
            if (next != null)
            {
                return next;
            }

            currentReaderIndex++;
            return ReadNext();
        }

        /// <summary>
        /// Reads the next sentence (and the wiki page where it has been parsed) in the wikimedia dump file
        /// </summary>
        /// <returns></returns>
        public Tuple<string, WikiPage> ReadNextInReader(XmlDumpFileReader xmlDumpFileReader)
        {
            if (this.stackedSentences.Any())
            {
                // Just pop the next sentence
                var nextSentence = this.stackedSentences[0];
                this.stackedSentences.RemoveAt(0);
                return new Tuple<string, WikiPage>(nextSentence, currentWikiPage);
            }

            // Get next wikipedia article
            currentWikiPage = xmlDumpFileReader.ReadNext(pageFilterer);
            this.WikiPageCounter++;
            
            // If we couldn't get the next page, then we reached the end of the current dump file
            if (currentWikiPage == null)
            {
                // We arrived at the end of the dump file
                return null;
            }
            
            // Cleanup article content
            var cleanedText = this.wikiMarkupCleaner.CleanArticleContent(currentWikiPage.Text);

            // Split in sentences
            var cleanedSentences = cleanedText
                .Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(line => sentenceDetector.SentenceDetect(line))
                .ToList();

            this.stackedSentences.AddRange(cleanedSentences);
            return this.ReadNext();
        }
    }
}
