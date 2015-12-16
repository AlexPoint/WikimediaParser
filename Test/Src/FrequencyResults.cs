using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using OpenNLP.Tools.Ling;
using WikitionaryDumpParser.Src;

namespace Test.Src
{
    public class FrequencyResults
    {
        private readonly Dictionary<WordOccurrence, long> WordFrequencies = new Dictionary<WordOccurrence, long>(new WordAndLocationComparer());
        private readonly Dictionary<WordOccurrence, long> ExcludedWordFrequencies = new Dictionary<WordOccurrence, long>(new WordAndLocationComparer());
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

        public void AddOccurrences(List<Tuple<WordOccurrence, long>> words, string pageTitle)
        {
            foreach (var word in words)
            {
                AddOccurence(word, pageTitle, words.IndexOf(word));
            }
        }

        private static readonly HashSet<string> WatchedWords = new HashSet<string>(new List<string>() { "UTC", "b", "C", "\\frac", "II", "f", "co", "P", "REDIRECT", 
            "\\mathbf", "J.", "O", "z", "\\right", "—Preceding", "De", "\\left", "mm", "POV", "NPOV", "\\sum_", "e^", "NOT", "birth_place", "WikiProjectBannerShell|1=", 
            "//en.wikipedia.org/w/index.php", "nm", "\\operatorname", "|image" });
        /*
         * Also check: 
         * - words with first letter capitalize and not beginning of sentence
         * - words with first letter not capitalized and beginning of sentence
        */

        /*
         * start with \ -> 2003 (/500k)
         * start with non char -> 
         */

        /*
         * TODO:
         * - list all weird words
         * - refactor parsing with different blocks (plug pre/post processing)
         * - regex with balance group
         */

        public void AddOccurence(Tuple<WordOccurrence,long> wordAndFreq, string pageTitle, int index)
        {
            if (WatchedWords.Contains(wordAndFreq.Item1.Word) || wordAndFreq.Item1.Word.EndsWith(".jpg"))
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

        public void WriteFiles(string keptWordsFilePath, string excludedWordsFilePath, string mergedWordsFilePath, string notFoundWordsFilePath)
        {
            var filterWordFrequencies = PostProcessWords(WordFrequencies, mergedWordsFilePath, notFoundWordsFilePath);

            // Word we kept in ferquency list
            var lines = filterWordFrequencies
                .OrderByDescending(wf => wf.Value)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Word, ent.Key.IsFirstLineToken, ent.Value));
            File.WriteAllLines(keptWordsFilePath, lines);

            // Word we excluded from frequency list
            var lines2 = ExcludedWordFrequencies
                .OrderByDescending(wf => wf.Value)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Word, ent.Key.IsFirstLineToken, ent.Value));
            File.WriteAllLines(excludedWordsFilePath, lines2);
        }


        public static Dictionary<WordOccurrence, long> PostProcessWords(Dictionary<WordOccurrence, long> wordOccurences, string mergedWordsFilePath, string notFoundWordsFilePath)
        {
            var updatedWordOccurrences = new List<Tuple<string, string>>();
            var occurencesToRemove = new List<WordOccurrence>();
            var notFoundOccurrences = new List<string>();

            // For each word occurence found at the beginning of a sentence, try to find a similar entry
            foreach (var wordOccurrence in wordOccurences.Where(ent => ent.Key.IsFirstLineToken))
            {
                var word = wordOccurrence.Key.Word;
                var lcOccurrence = new WordOccurrence()
                {
                    Word = StringHelpers.LowerCaseFirstLetter(word),
                    IsFirstLineToken = false
                };

                var ucOccurrence = new WordOccurrence()
                {
                    Word = StringHelpers.UpperCaseFirstLetter(word),
                    IsFirstLineToken = false
                };

                long lcFreq;
                long ucFreq;
                if (wordOccurences.TryGetValue(lcOccurrence, out lcFreq) && wordOccurences.TryGetValue(ucOccurrence, out ucFreq))
                {
                    // Increase the counter of the most likely occurrence
                    if (lcFreq >= ucFreq)
                    {
                        wordOccurences[lcOccurrence] += wordOccurrence.Value;
                        updatedWordOccurrences.Add(new Tuple<string, string>(lcOccurrence.Word, word));
                    }
                    else
                    {
                        wordOccurences[ucOccurrence] += wordOccurrence.Value;
                        updatedWordOccurrences.Add(new Tuple<string, string>(ucOccurrence.Word, word));
                    }
                    occurencesToRemove.Add(wordOccurrence.Key);
                }
                else if (wordOccurences.TryGetValue(lcOccurrence, out lcFreq))
                {
                    wordOccurences[lcOccurrence] += wordOccurrence.Value;
                    updatedWordOccurrences.Add(new Tuple<string, string>(lcOccurrence.Word, word));
                    occurencesToRemove.Add(wordOccurrence.Key);
                }
                else if (wordOccurences.TryGetValue(ucOccurrence, out ucFreq))
                {
                    wordOccurences[ucOccurrence] += wordOccurrence.Value;
                    updatedWordOccurrences.Add(new Tuple<string, string>(ucOccurrence.Word, word));
                    occurencesToRemove.Add(wordOccurrence.Key);
                }
                else
                {
                    // 
                    notFoundOccurrences.Add(word);
                }   
            }

            // Write merged words in specific file
            var mergedWordsLines = updatedWordOccurrences
                .Select(tup => string.Format("{0} -> {1}", tup.Item2, tup.Item1));
            File.WriteAllLines(mergedWordsFilePath, mergedWordsLines);

            // Write not found words in specific file
            File.WriteAllLines(mergedWordsFilePath, notFoundOccurrences);

            // remove the merged occurrences
            foreach (var occurrenceToRemove in occurencesToRemove)
            {
                wordOccurences.Remove(occurrenceToRemove);
            }

            return wordOccurences;
        }
    }

    public class WordAndLocationComparer : IEqualityComparer<WordOccurrence>
    {
        public bool Equals(WordOccurrence x, WordOccurrence y)
        {
            return (x == null && y == null)
                   || (x != null && y != null && x.Word == y.Word && x.IsFirstLineToken == y.IsFirstLineToken);
        }

        public int GetHashCode(WordOccurrence obj)
        {
            if (obj != null)
            {
                return obj.Word.GetHashCode() * 17 + obj.IsFirstLineToken.GetHashCode();
            }
            return -1;
        }
    }
}
