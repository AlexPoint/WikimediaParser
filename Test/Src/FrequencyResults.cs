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
            Console.WriteLine("Loaded {0} word occurrences", lines.Count());
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
