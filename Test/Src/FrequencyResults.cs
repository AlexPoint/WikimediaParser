using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenNLP.Tools.Ling;

namespace Test.Src
{
    public class FrequencyResults
    {
        private readonly Dictionary<WordAndFrequency, long> WordFrequencies = new Dictionary<WordAndFrequency, long>(new WordAndLocationComparer());
        private readonly Dictionary<WordAndFrequency, long> ExcludedWordFrequencies = new Dictionary<WordAndFrequency, long>(new WordAndLocationComparer());
        private readonly Regex HasEnglishLetterRegex = new Regex(@"[a-zA-Z]+", RegexOptions.Compiled);

        private static FrequencyResults _instance;
        public static FrequencyResults Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FrequencyResults();
                }
                return _instance;
            }
        }

        private FrequencyResults() { }


        // Methods -----------------------

        public void AddOccurrences(List<Tuple<WordAndFrequency, long>> words, string pageTitle)
        {
            foreach (var word in words)
            {
                AddOccurence(word, pageTitle, words.IndexOf(word));
            }
        }

        private static readonly HashSet<string> WatchedWords = new HashSet<string>(new List<string>(){"align=", "s.", "&nbsp", "it.", "|style=", "File","&ndash", "b.", "d.", "s"});

        public void AddOccurence(Tuple<WordAndFrequency,long> wordAndFreq, string pageTitle, int index)
        {
            if (WatchedWords.Contains(wordAndFreq.Item1.Word))
            {
                WatchedWords.Remove(wordAndFreq.Item1.Word);
                // Log only the first time
                Console.WriteLine("'{0}' found in '{1}' at index {2}", wordAndFreq, pageTitle, index);
            }

            var relevantDictionary = HasEnglishLetterRegex.IsMatch(wordAndFreq.Item1.Word)
                ? WordFrequencies
                : ExcludedWordFrequencies;

            var alreadyExist = relevantDictionary.ContainsKey(wordAndFreq.Item1);
            if (alreadyExist)
            {
                relevantDictionary[wordAndFreq.Item1] += wordAndFreq.Item2;
            }
            else
            {
                relevantDictionary.Add(wordAndFreq.Item1, wordAndFreq.Item2);
            }
        }

        public void WriteFiles(string keptWordsFilePath, string excludedWordsFilePath)
        {
            // Word we kept in ferquency list
            var lines = WordFrequencies
                .OrderByDescending(wf => wf.Value)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Word, ent.Key.IsFirstLineToken, ent.Value));
            File.WriteAllLines(keptWordsFilePath, lines);

            // Word we excluded from frequency list
            var lines2 = ExcludedWordFrequencies
                .OrderByDescending(wf => wf.Value)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Word, ent.Key.IsFirstLineToken, ent.Value));
            File.WriteAllLines(excludedWordsFilePath, lines2);
        }
    }

    public class WordAndLocationComparer : IEqualityComparer<WordAndFrequency>
    {
        public bool Equals(WordAndFrequency x, WordAndFrequency y)
        {
            return (x == null && y == null)
                   || (x != null && y != null && x.Word == y.Word && x.IsFirstLineToken == y.IsFirstLineToken);
        }

        public int GetHashCode(WordAndFrequency obj)
        {
            if (obj != null)
            {
                return obj.Word.GetHashCode() * 17 + obj.IsFirstLineToken.GetHashCode();
            }
            return -1;
        }
    }
}
