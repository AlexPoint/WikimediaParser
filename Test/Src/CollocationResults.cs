using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public class CollocationResults
    {
        // Properties ---------------

        public long TotalWordCounter { get; set; }
        public Dictionary<string, long> WordFrequencies { get; set; }
        public Dictionary<string, TopWordAndCounter> Bigrams { get; set; }

        // Constructor --------------

        public CollocationResults()
        {
            this.Bigrams = new Dictionary<string, TopWordAndCounter>();
            this.WordFrequencies = new Dictionary<string, long>();
        }

        // Methods ------------------

        private void IncreaseWordFreq(string word)
        {
            TotalWordCounter++;
            if (WordFrequencies.ContainsKey(word))
            {
                WordFrequencies[word]++;
            }
            else
            {
                WordFrequencies.Add(word, 1);
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

                if (Bigrams.ContainsKey(next))
                {
                    Bigrams[next].CounterWhenAfterAnotherWord++;
                    var existingDic = Bigrams[next].PreviousWordsAndFrequencies;
                    if (existingDic.ContainsKey(current))
                    {
                        existingDic[current]++;
                    }
                    else
                    {
                        existingDic.Add(current, 1);
                    }
                }
                else
                {
                    Bigrams.Add(next, new TopWordAndCounter()
                    {
                        CounterWhenAfterAnotherWord = 1,
                        PreviousWordsAndFrequencies = new Dictionary<string, long>() {{ current, 1}}
                    });
                }
            }
        }

        public List<Tuple<string, string, double>> ComputePMIs(int collocationFrequencyFilter)
        {
            var results = new List<Tuple<string, string, double>>();

            foreach (var bigram in Bigrams)
            {
                var topWordAndCounter = bigram.Value;
                foreach (var previousWord in topWordAndCounter.PreviousWordsAndFrequencies)
                {
                    if (collocationFrequencyFilter <= previousWord.Value)
                    {
                        var nbOfX = WordFrequencies[bigram.Key];
                        var pmi = ComputePMI(previousWord.Value, topWordAndCounter.CounterWhenAfterAnotherWord, nbOfX, TotalWordCounter);
                        results.Add(new Tuple<string, string, double>(previousWord.Key, bigram.Key, pmi)); 
                    }
                }
            }

            return results;
        }

        private static double ComputePMI(long nbOfXAfterY, long nbOfXAfterAnything, long nbOfX, long nbOfWords)
        {
            return Math.Log10(((double)nbOfXAfterY / nbOfXAfterAnything) / ((double)nbOfX / nbOfWords));
        }

        public void SaveCollocationPMIs(string filePath, int collocationFrequencyFilter)
        {
            var lines = ComputePMIs(collocationFrequencyFilter)
                .OrderByDescending(tup => tup.Item3)
                .Select(tup => string.Format("{0}|{1}|{2}", tup.Item1, tup.Item2, tup.Item3));
            File.WriteAllLines(filePath, lines);
        }
    }

    public class TopWordAndCounter
    {
        // Properties --------------

        public long CounterWhenAfterAnotherWord { get;set; }
        public Dictionary<string, long> PreviousWordsAndFrequencies { get; set; }


        // Constructors ------------

        public TopWordAndCounter()
        {
            PreviousWordsAndFrequencies = new Dictionary<string, long>();
        }
    }
}
