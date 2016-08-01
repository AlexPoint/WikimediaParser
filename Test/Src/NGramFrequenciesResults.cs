using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MoreLinq;

namespace Test.Src
{
    public class NGramFrequenciesResults
    {
        private const char Separator = '|';

        // Properties ---------------

        public int N { get; set; }
        public long TotalWordCounter { get; set; }
        public Dictionary<string, long> WordFrequencies { get; set; }
        public long TotalNgramsCounter { get; set; }
        public Dictionary<string[], long> NGramsFrequencies { get; set; }

        // Constructor --------------

        public NGramFrequenciesResults(int n)
        {
            this.N = n;
            this.NGramsFrequencies = new Dictionary<string[], long>(new NgramEqualityComparer());
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
                WordFrequencies.Add(word, frequency);
            }
        }

        private void AddNGram(string[] ngram, long frequency = 1, bool increaseTotalNGramCounter = true)
        {
            if (increaseTotalNGramCounter)
            {
                TotalNgramsCounter += frequency; 
            }

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
            // Don't take into account sentences with less than N characters (no N-gram possible)
            if (sentence.Length < N)
            {
                return;
            }

            // Increase the counter for each word individually
            foreach (var token in sentence)
            {
                IncreaseWordFreq(token);
            }

            // Browse all the n-grams of the sentence
            for (var i = 0; i < sentence.Length - N; i++)
            {
                var ngram = sentence.Skip(i).Take(N).ToArray();
                AddNGram(ngram);
            }
        }

        public List<Tuple<string[], double>> ComputePMIs(int collocationFrequencyFilter)
        {
            var results = new List<Tuple<string[], double>>();

            foreach (var tupleFrequency in NGramsFrequencies.Where(tup => collocationFrequencyFilter <= tup.Value))
            {
                var counts = tupleFrequency.Key.Select(k => WordFrequencies[k]).ToArray();
                var pmi = ComputePMI(counts, TotalWordCounter, tupleFrequency.Value, TotalNgramsCounter);
                results.Add(new Tuple<string[], double>(tupleFrequency.Key, pmi));
            }

            return results;
        }

        private static double ComputePMI(long[] wordCounts, long nbOfWords, long ngramCount, long nbOfBigrams)
        {
            double denominator = 1;
            for (int i = 0; i < wordCounts.Length; i++)
            {
                denominator *= (double)wordCounts[i]/nbOfWords;
            }
            return Math.Log10(((double) ngramCount/nbOfBigrams)/denominator);
        }

        public void SaveNGramsFrequencies(string filePath, int frequencyFilter)
        {
            var lines = this.NGramsFrequencies
                .Where(tup => frequencyFilter <= tup.Value)
                .OrderByDescending(tup => tup.Value)
                .Select(ent => string.Format("{0}|{1}", string.Join(Separator.ToString(), ent.Key), ent.Value));
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
                .OrderByDescending(tup => tup.Item2)
                .Select(tup => string.Format("{0}|{1}", string.Join(Separator.ToString(), tup.Item1), tup.Item2));
            File.WriteAllLines(filePath, lines);
        }

        public void SaveResults(string wordFrequenciesFilePath, string ngramsFrequenciesFilePath)
        {
            // Persist word frequencies
            using (var writer = new StreamWriter(wordFrequenciesFilePath))
            {
                var lines = WordFrequencies
                    .OrderByDescending(ent => ent.Value)
                    .Select(ent => string.Format("{0}{1}{2}", ent.Key, Separator, ent.Value));
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }
            
            // Persist ngrams frequencies
            using (var writer = new StreamWriter(ngramsFrequenciesFilePath))
            {
                // First line is the ngrams counter
                writer.WriteLine(TotalNgramsCounter);
                // Don't try to order NGramsFrequencies since it causes OutOfMemoryExceptions (ordering a dictionary creates an ordered copy in all cases)
                foreach (var freq in NGramsFrequencies)
                {
                    var sb = new StringBuilder();
                    sb.Append(string.Join(Separator.ToString(), freq.Key)).Append(Separator).Append(freq.Value);
                    writer.WriteLine(sb.ToString());
                }
            }
        }

        public void LoadResults(string wordFrequenciesFilePath, string ngramsFrequenciesFilePath)
        {
            // Load word frequencies
            using (var reader = new StreamReader(File.OpenRead(wordFrequenciesFilePath)))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    var parts = line.Split(Separator);
                    if (parts.Length == 2)
                    {
                        IncreaseWordFreq(parts[0], long.Parse(parts[1]));
                    }
                }
            }

            // Load ngrams frequencies
            using (var reader = new StreamReader(File.OpenRead(ngramsFrequenciesFilePath)))
            {
                var firstLine = reader.ReadLine();
                var ngramCounter = int.Parse(firstLine);
                TotalNgramsCounter += ngramCounter;
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    var parts = line.Split(Separator);
                    if (parts.Length == (N + 1))
                    {
                        var ngram = parts.Take(parts.Length - 1).ToArray();
                        AddNGram(ngram, long.Parse(parts.Last()), false);
                    }
                }
            }
        }

        public int FlushNgramsWithFrequencyBelow(int minFrequency)
        {
            var count = 0;
            var batches = NGramsFrequencies
                .Where(ent => ent.Value < minFrequency)
                .Select(ent => ent.Key)
                .Batch(1000)
                .ToList();
            foreach (var batch in batches)
            {
                foreach (var entryToRemove in batch)
                {
                    NGramsFrequencies.Remove(entryToRemove);
                    count++;
                }
            }

            Console.WriteLine("NGramFrequencies dictionary size: {0}", NGramsFrequencies.Count);
            
            return count;
        }
    }

    public class NgramEqualityComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(string[] obj)
        {
            int result = 17;
            for (int i = 0; i < obj.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i].GetHashCode();
                }
            }
            return result;
        }
    }
}
