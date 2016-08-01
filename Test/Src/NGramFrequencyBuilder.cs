using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public class NGramFrequencyBuilder
    {
        private int N { get; set; }
        private string PathToDownloadDirectory { get; set; }
        private int NbOfSentencesToParse { get; set; }
        private int FlushMinFrequency { get; set; }
        private int FlushNbOfSentences { get; set; }

        public NGramFrequencyBuilder(int n, string pathToDownloadDirectory, int nbOfSentencesToParse, int flushMinFrequency, int flushNbOfSentences)
        {
            N = n;
            PathToDownloadDirectory = pathToDownloadDirectory;
            NbOfSentencesToParse = nbOfSentencesToParse;
            FlushMinFrequency = flushMinFrequency;
            FlushNbOfSentences = flushNbOfSentences;
        }

        // Methods -------------

        public void ComputeNgramsFrequencies()
        {
            var result = new NGramFrequenciesResults(N);

            var nbOfAlreadyParsedSentences = 0;
            var ngramsDirectory = this.PathToDownloadDirectory + "ngrams";
            if (!Directory.Exists(ngramsDirectory))
            {
                Directory.CreateDirectory(ngramsDirectory);
            }

            var ngramDirectory = ngramsDirectory + string.Format("/{0}-gram", N);
            if (!Directory.Exists(ngramDirectory))
            {
                Directory.CreateDirectory(ngramDirectory);
            }

            var wordFrequencyFilePath = ngramDirectory + "/word-frequencies.txt";
            var ngramFreqFilePath = ngramDirectory + "/ngrams-frequencies.txt";
            var nbOfSentencesParsedFilePath = ngramDirectory + "/nbOfSentencesParsed.txt";
            var parsingResumed = false;
            if (File.Exists(nbOfSentencesParsedFilePath))
            {
                int nbOfSentencesParsed;
                if (int.TryParse(File.ReadAllText(nbOfSentencesParsedFilePath), out nbOfSentencesParsed))
                {
                    Console.WriteLine("{0} sentences have already been parsed. Resume parsing? (y/n)", nbOfSentencesParsed);
                    var resumeParsing = string.Equals(Console.ReadLine(), "Y", StringComparison.InvariantCultureIgnoreCase);
                    if (resumeParsing)
                    {
                        nbOfAlreadyParsedSentences = nbOfSentencesParsed;
                        parsingResumed = true;
                    }
                }
            }

            // Final frequency list
            Console.WriteLine("Load frequency list");
            var frequencyDirectory = PathToDownloadDirectory + "frequencies";
            var frequencyListPath = frequencyDirectory + "/frequency-list - 150m.txt";
            var freqDic = new Dictionary<string, long>();
            using (var reader = new StreamReader(File.OpenRead(frequencyListPath)))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        freqDic.Add(string.Intern(parts[0]), long.Parse(parts[1]));
                    }

                    line = reader.ReadLine();
                }
            }

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Start computing {0}-grams frequencies", N);

            // Tokenize the sentences and compute the frequencies
            Func<string[], int, bool> extractTokens = (tokens, sentenceCounter) =>
            {
                if (sentenceCounter % FlushNbOfSentences == 0)
                {
                    var nbOfFlushedNGrams = result.FlushNgramsWithFrequencyBelow(FlushMinFrequency);
                    Console.WriteLine("Flushed {0} ngrams with frequency below {1}", nbOfFlushedNGrams, FlushMinFrequency);
                }

                // Lowercase the first token if necessary
                if (tokens.Length > 0 && !string.IsNullOrEmpty(tokens[0]) && char.IsLetter(tokens[0][0]))
                {
                    long freq;
                    long lcFreq;
                    if (freqDic.TryGetValue(tokens[0], out freq) && freqDic.TryGetValue(Utilities.LowerCaseFirstLetter(tokens[0]), out lcFreq) && lcFreq > freq)
                    {
                        tokens[0] = Utilities.LowerCaseFirstLetter(tokens[0]);
                    }
                }
                result.AddBigrams(tokens);
                return true;
            };
            Utilities.ExtractTokensFromTxtFiles(extractTokens, NbOfSentencesToParse, nbOfAlreadyParsedSentences);

            // Final flushing
            Console.WriteLine("Flushed {0} ngrams with frequency below {1}", result.FlushNgramsWithFrequencyBelow(FlushMinFrequency), FlushMinFrequency);

            // Load previous frequency dictionaries that were already computed
            Console.WriteLine("Loading previous results");
            if (parsingResumed)
            {
                result.LoadResults(wordFrequencyFilePath, ngramFreqFilePath);
            }

            // Save results on disk for later
            Console.WriteLine("Saving results on disk");
            result.SaveResults(wordFrequencyFilePath, ngramFreqFilePath);

            // Save the nb of sentences parsed (for information and being able to relaunch the parsing at this point)
            File.WriteAllText(nbOfSentencesParsedFilePath, NbOfSentencesToParse.ToString());

            Console.WriteLine("Finished computing {0}-grams frequencies", N);
            Console.WriteLine("=====================================");

            sw.Stop();
            Console.WriteLine("Ellapsed time: {0}", sw.Elapsed.ToString("g"));
        }
    }
}
