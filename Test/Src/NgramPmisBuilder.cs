using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public class NgramPmisBuilder
    {
        private int N { get; set; }
        private int FrequencyFilter { get; set; }

        public NgramPmisBuilder(int n, int frequencyFilter)
        {
            N = n;
            FrequencyFilter = frequencyFilter;
        }

        public void ComputePmis()
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Start post processing ngrams frequencies");

            // Load results
            var result = new NGramFrequenciesResults(N);

            var ngramsDirectory = Utilities.PathToDownloadDirectory + "/ngrams";
            var ngramDirectory = ngramsDirectory + string.Format("/{0}-gram", N);
            if (!Directory.Exists(ngramDirectory))
            {
                Directory.CreateDirectory(ngramDirectory);
            }
            var wordFrequencyFilePath = ngramDirectory + "/word-frequencies.txt";
            var ngramFreqFilePath = ngramDirectory + "/ngrams-frequencies.txt";
            result.LoadResults(wordFrequencyFilePath, ngramFreqFilePath);

            // Save frequency files on disk
            var ngramsPmisFilePath = ngramDirectory + "/ngrams-pmis.txt";
            result.SaveCollocationPMIs(ngramsPmisFilePath, FrequencyFilter);

            Console.WriteLine("Done post processing ngrams frequencies");
            sw.Stop();
            Console.WriteLine("Executed in {0}", sw.Elapsed.ToString("g"));
        }
    }
}
