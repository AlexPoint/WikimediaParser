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

        private static readonly HashSet<string> WatchedWords = new HashSet<string>(new List<string>() { "*The", "/female" });
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
            /*if (WatchedWords.Contains(wordAndFreq.Item1.Word) || wordAndFreq.Item1.Word.EndsWith(".jpg"))
            {
                WatchedWords.Remove(wordAndFreq.Item1.Word);
                // Log only the first time
                Console.WriteLine("'{0}' found in '{1}' at index {2}", wordAndFreq, pageTitle, index);
            }*/

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
                .Select(ent => string.Format("{0}|{1}", ent.Key.Word, ent.Value));
            File.WriteAllLines(keptWordsFilePath, lines);

            // Word we excluded from frequency list
            var lines2 = ExcludedWordFrequencies
                .OrderByDescending(wf => wf.Value)
                .Select(ent => string.Format("{0}|{1}", ent.Key.Word, ent.Value));
            File.WriteAllLines(excludedWordsFilePath, lines2);
        }


        public static Dictionary<WordOccurrence, long> PostProcessWords(Dictionary<WordOccurrence, long> wordOccurences, string mergedWordsFilePath, string notFoundWordsFilePath)
        {
            var updatedWordOccurrences = new List<Tuple<WordOccurrence, WordOccurrence>>();
            var notFoundOccurrences = new List<WordOccurrence>();

            // For each word occurence found at the beginning of a sentence, try to find a similar entry
            foreach (var wordOccurrence in wordOccurences.Where(ent => ent.Key.IsFirstTokenInSentence))
            {
                var word = wordOccurrence.Key.Word;

                var nonFirstTokenOccurrence = new WordOccurrence()
                {
                    Word = word,
                    IsFirstTokenInSentence = false
                };
                long freq;
                if (wordOccurences.TryGetValue(nonFirstTokenOccurrence, out freq))
                {
                    // We found the exact same occurrence but not at the beginning of a sentence -> merge the two frequencies
                    updatedWordOccurrences.Add(new Tuple<WordOccurrence, WordOccurrence>(nonFirstTokenOccurrence,
                        wordOccurrence.Key));
                }
                else
                {
                    var relatedOccurrence = new WordOccurrence()
                    {
                        Word = StringHelpers.IsFirstLetterLower(word)
                                ? StringHelpers.UpperCaseFirstLetter(word)
                                : StringHelpers.LowerCaseFirstLetter(word),
                        IsFirstTokenInSentence = false
                    };
                    if (wordOccurences.TryGetValue(relatedOccurrence, out freq))
                    {
                        // We found the exact same occurrence but not at the beginning of a sentence -> merge the two frequencies
                        updatedWordOccurrences.Add(new Tuple<WordOccurrence, WordOccurrence>(relatedOccurrence,
                            wordOccurrence.Key));
                    }
                    else
                    {
                        // 
                        notFoundOccurrences.Add(wordOccurrence.Key);
                    }
                }

                /*var lcOccurrence = new WordOccurrence()
                {
                    Word = StringHelpers.LowerCaseFirstLetter(word),
                    IsFirstTokenInSentence = false
                };

                var ucOccurrence = new WordOccurrence()
                {
                    Word = StringHelpers.UpperCaseFirstLetter(word),
                    IsFirstTokenInSentence = false
                };

                long lcFreq;
                long ucFreq;
                if (wordOccurences.TryGetValue(lcOccurrence, out lcFreq) && wordOccurences.TryGetValue(ucOccurrence, out ucFreq))
                {
                    // We found both a lower and
                    updatedWordOccurrences.Add(new Tuple<WordOccurrence, WordOccurrence>(lcOccurrence, wordOccurrence.Key));

                    // If the word is uppercased, and there is also an uppercased occurrence with first token = false, merge it with the lower cased one
                    if (char.IsUpper(word[0]) && wordOccurences.TryGetValue(ucOccurrence, out ucFreq))
                    {
                        updatedWordOccurrences.Add(new Tuple<WordOccurrence, WordOccurrence>(lcOccurrence, ucOccurrence));
                    }
                }
                else if(wordOccurences.TryGetValue(lcOccurrence, out lcFreq))
                {
                    
                }
                else if (wordOccurences.TryGetValue(ucOccurrence, out ucFreq))
                {
                    updatedWordOccurrences.Add(new Tuple<WordOccurrence, WordOccurrence>(ucOccurrence, wordOccurrence.Key));
                }
                else
                {
                    // 
                    notFoundOccurrences.Add(word);
                }*/
            }
            
            // remove the merged occurrences
            foreach (var updatedOccurrence in updatedWordOccurrences)
            {
                // updates the frequency of the main occurrence
                wordOccurences[updatedOccurrence.Item1] += wordOccurences[updatedOccurrence.Item2];
                // remove the entry for the second occurrence
                wordOccurences.Remove(updatedOccurrence.Item2);
            }
            // remove the not found occurrences
            foreach (var notFoundOccurrence in notFoundOccurrences)
            {
                wordOccurences.Remove(notFoundOccurrence);
            }

            // Write merged words in specific file
            var mergedWordsLines = updatedWordOccurrences
                .Select(tup => string.Format("{0} -> {1}", tup.Item2, tup.Item1));
            File.WriteAllLines(mergedWordsFilePath, mergedWordsLines);

            // Write not found words in specific file
            File.WriteAllLines(notFoundWordsFilePath, notFoundOccurrences.Select(occ => occ.Word));


            var wordOccurrencesToMerge = new List<Tuple<WordOccurrence, WordOccurrence>>();

            // Now we do'nt have any entity marked as first token in sentence.
            // Merge all the related entities (same case) but with one occurrence begin far less frequent than the other.
            // Ex: "American" and "american" -> "american" is present 3 times whereas "American" is present more than 1000 times
            var groupsToMerge = wordOccurences.GroupBy(wo => StringHelpers.LowerCaseFirstLetter(wo.Key.Word)).Where(grp => grp.Count() > 1).ToList();
            foreach (var group in groupsToMerge)
            {
                var occurrenceWithHighestFreq = group.OrderByDescending(grp => grp.Value).First();
                foreach (var occurrence in group.OrderByDescending(grp => grp.Value).Skip(1))
                {
                    if (occurrence.Value*20 < occurrenceWithHighestFreq.Value)
                    {
                        wordOccurrencesToMerge.Add(new Tuple<WordOccurrence, WordOccurrence>(occurrenceWithHighestFreq.Key, occurrence.Key));
                    }
                }
            }
            // remove the merged occurrences
            foreach (var updatedOccurrence in wordOccurrencesToMerge)
            {
                // updates the frequency of the main occurrence
                wordOccurences[updatedOccurrence.Item1] += wordOccurences[updatedOccurrence.Item2];
                // remove the entry for the second occurrence
                wordOccurences.Remove(updatedOccurrence.Item2);
            }
            

            return wordOccurences;
        }
    }

    public class WordAndLocationComparer : IEqualityComparer<WordOccurrence>
    {
        public bool Equals(WordOccurrence x, WordOccurrence y)
        {
            return (x == null && y == null)
                   || (x != null && y != null && x.Word == y.Word && x.IsFirstTokenInSentence == y.IsFirstTokenInSentence);
        }

        public int GetHashCode(WordOccurrence obj)
        {
            if (obj != null)
            {
                return obj.Word.GetHashCode() * 17 + obj.IsFirstTokenInSentence.GetHashCode();
            }
            return -1;
        }
    }
}
