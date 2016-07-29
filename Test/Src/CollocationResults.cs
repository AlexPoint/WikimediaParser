using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public class CollocationResults
    {
        // Properties ---------------

        public long TotalWordCounter { get; set; }
        public Dictionary<string, long> WordFrequencies { get; set; }
        public long TotalNgramsCounter { get; set; }
        public Dictionary<Tuple<string, string>, long> NGramsFrequencies { get; set; }

        // Constructor --------------

        public CollocationResults()
        {
            this.NGramsFrequencies = new Dictionary<Tuple<string, string>, long>();
            this.WordFrequencies = new Dictionary<string, long>();
        }

        // Methods ------------------

        private void IncreaseWordFreq(string word, long frequency = 1)
        {
            TotalWordCounter += frequency;
            if (WordFrequencies.ContainsKey(word))
            {
                WordFrequencies[word]++;
            }
            else
            {
                WordFrequencies.Add(word, 1);
            }
        }

        private void AddNGram(Tuple<string, string> ngram, long frequency = 1)
        {
            TotalNgramsCounter += frequency;
            if (NGramsFrequencies.ContainsKey(ngram))
            {
                NGramsFrequencies[ngram]++;
            }
            else
            {
                NGramsFrequencies.Add(ngram, frequency);
            }
        }

        public void AddBigrams(string[] sentence)
        {
            // Don't take into account sentences with less than 2 characters
            if (sentence.Length < 2)
            {
                return;
            }

            // Increase the counter for each word individually
            foreach (var token in sentence)
            {
                IncreaseWordFreq(token);
            }

            // Browse all the bigrams of the sentence
            for (var i = 0; i < sentence.Length - 2; i++)
            {
                var current = sentence[i];
                var next = sentence[i+1];
                var ngram = new Tuple<string, string>(current, next);

                AddNGram(ngram);
            }
        }

        public List<Tuple<string, string, double>> ComputePMIs(int collocationFrequencyFilter)
        {
            var results = new List<Tuple<string, string, double>>();

            foreach (var tupleFrequency in NGramsFrequencies.Where(tup => collocationFrequencyFilter <= tup.Value))
            {
                var freq1 = WordFrequencies[tupleFrequency.Key.Item1];
                var freq2 = WordFrequencies[tupleFrequency.Key.Item2];
                var pmi = ComputePMI(freq1, freq2, TotalWordCounter, tupleFrequency.Value, TotalNgramsCounter);
                results.Add(new Tuple<string, string, double>(tupleFrequency.Key.Item1, tupleFrequency.Key.Item2, pmi));
            }

            return results;
        }

        private static double ComputePMI(long word1Count, long word2Count, long nbOfWords, long bigramCount, long nbOfBigrams)
        {
            return
                Math.Log10(((double) bigramCount/nbOfBigrams)/((double) (word1Count*word2Count)/(nbOfWords*nbOfWords)));
        }

        public void SaveNGramsFrequencies(string filePath, int frequencyFilter)
        {
            var lines = this.NGramsFrequencies
                .Where(tup => frequencyFilter <= tup.Value)
                .OrderByDescending(tup => tup.Value)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Item1, ent.Key.Item2, ent.Value));
            using (var writer = new StreamWriter(File.OpenWrite(filePath)))
            {
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }

        public void SaveCollocationPMIs(string filePath, int collocationFrequencyFilter)
        {
            var lines = ComputePMIs(collocationFrequencyFilter)
                .OrderByDescending(tup => tup.Item3)
                .Select(tup => string.Format("{0}|{1}|{2}", tup.Item1, tup.Item2, tup.Item3));
            File.WriteAllLines(filePath, lines);
        }

        public void SaveResults(string wordFrequenciesFilePath, string ngramsFrequenciesFilePath)
        {
            // Persist word frequencies
            using (var writer = new StreamWriter(wordFrequenciesFilePath))
            {
                var lines = WordFrequencies
                    .OrderByDescending(ent => ent.Value)
                    .Select(ent => string.Format("{0}|{1}", ent.Key, ent.Value));
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }
            
            // Persist ngrams frequencies
            using (var writer = new StreamWriter(ngramsFrequenciesFilePath))
            {
                var lines = NGramsFrequencies
                    .OrderByDescending(ent => ent.Value)
                    .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Item1, ent.Key.Item2, ent.Value));
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }

        public void LoadResults(string wordFrequenciesFilePath, string ngramsFrequenciesFilePath)
        {
            // Load word frequencies
            using (var reader = new StreamReader(File.OpenRead(wordFrequenciesFilePath)))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        IncreaseWordFreq(parts[0], long.Parse(parts[1]));
                    }

                    line = reader.ReadLine();
                }
            }

            // Load ngrams frequencies
            using (var reader = new StreamReader(File.OpenRead(ngramsFrequenciesFilePath)))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3)
                    {
                        var ngram = new Tuple<string, string>(parts[0], parts[1]);
                        AddNGram(ngram, long.Parse(parts[2]));
                    }

                    line = reader.ReadLine();
                }
            }
        }
    }
}
