using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Test.Src
{
    public class FrequencyResults
    {
        public readonly Dictionary<WordOccurrence, long> WordFrequencies = new Dictionary<WordOccurrence, long>(new WordAndLocationComparer());
        public readonly Dictionary<WordOccurrence, long> ExcludedWordFrequencies = new Dictionary<WordOccurrence, long>(new WordAndLocationComparer());

        // Constructors ------------------

        public FrequencyResults()
        {
        }


        // Methods -----------------------
        
        public void AddOccurence(WordOccurrence token, long frequency = 1)
        {
            var isTokenValid = true;
            var hasEnglishLetter = false;
            // Only a-z, A-Z, - and . are allowed
            foreach (var c in token.Word)
            {
                if ((65 <= c && c <= 90) || (97 <= c && c <= 122))
                {
                    hasEnglishLetter = true;
                }
                else if (c == 39 || c == 45 || c == 46)
                {
                    // character is valid but it's not a letter
                }
                else
                {
                    isTokenValid = false;
                    break;
                }
            }

            if (!isTokenValid && !hasEnglishLetter)
            {
                return;
            }

            var relevantDictionary = isTokenValid && hasEnglishLetter ? WordFrequencies : ExcludedWordFrequencies;

            var alreadyExist = relevantDictionary.ContainsKey(token);
            if (alreadyExist)
            {
                relevantDictionary[token] += frequency;
            }
            else
            {
                relevantDictionary.Add(token, frequency);
            }
        }

        public void SaveFrequencyDictionary(string filePath)
        {
            var lines = WordFrequencies
                .OrderByDescending(d => d.Value)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Word, ent.Key.IsFirstTokenInSentence, ent.Value));
            File.WriteAllLines(filePath, lines);
        }

        public void SaveExcludedFrequencyDictionary(string filePath)
        {
            var lines = ExcludedWordFrequencies
                .OrderByDescending(d => d.Value)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Key.Word, ent.Key.IsFirstTokenInSentence, ent.Value));
            File.WriteAllLines(filePath, lines);
        }

        public void LoadFrequencyDictionary(string filePath, int minimumFrequency = 0)
        {
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length == 3)
                {
                    var wordOccurrence = new WordOccurrence()
                    {
                        Word = parts[0],
                        IsFirstTokenInSentence = bool.Parse(parts[1])
                    };
                    var freq = long.Parse(parts[2]);
                    if (minimumFrequency <= freq)
                    {
                        this.AddOccurence(wordOccurrence, freq); 
                    }
                }
            }
        }

        /*public void WriteFiles(string keptWordsFilePath, string excludedWordsFilePath, string mergedWordsFilePath, string notFoundWordsFilePath)
        {
            var filterWordFrequencies = PostProcessWords(WordFrequencies, mergedWordsFilePath, notFoundWordsFilePath);

            // Word we kept in frequency list
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

        public Dictionary<WordOccurrence, long> BuildFrequencyDictionary(string mergedWordsFilePath, string notFoundWordsFilePath)
        {
            // 
            var copy = new Dictionary<WordOccurrence, long>(this.WordFrequencies, new WordAndLocationComparer());
            var dictionary = PostProcessWords(copy, mergedWordsFilePath, notFoundWordsFilePath);
            // Empty word frequencies
            this.WordFrequencies.Clear();

            return dictionary;
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

            // Now we don't have any entity marked as first token in sentence.
            // Merge all the related entities (same case) but with one occurrence begin far less frequent than the other.
            // Ex: "American" and "american" -> "american" is present 3 times whereas "American" is present more than 1000 times
            var groupsToMerge = wordOccurences.GroupBy(wo => StringHelpers.LowerCaseFirstLetter(wo.Key.Word)).Where(grp => grp.Count() > 1).ToList();
            foreach (var group in groupsToMerge)
            {
                var occurrenceWithHighestFreq = group.OrderByDescending(grp => grp.Value).First();
                foreach (var occurrence in group.OrderByDescending(grp => grp.Value).Skip(1))
                {
                    // If more than 50 occurrences -> this occurrence is likely the best one
                    if (occurrenceWithHighestFreq.Value > 50)
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
        }*/
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
