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
        private readonly Dictionary<string, WordAndFrequency> WordFrequencies = new Dictionary<string, WordAndFrequency>();
        private readonly Dictionary<string, WordAndFrequency> ExcludedWordFrequencies = new Dictionary<string, WordAndFrequency>();
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

        public void AddOccurrences(IEnumerable<string> words, string pageTitle)
        {
            foreach (var word in words)
            {
                AddOccurence(word, pageTitle);
            }
        }

        public void AddOccurence(string word, string pageTitle)
        {
            var relevantDictionary = HasEnglishLetterRegex.IsMatch(word)
                ? WordFrequencies
                : ExcludedWordFrequencies;

            WordAndFrequency existingWordAndFrequency;
            var alreadyExist = relevantDictionary.TryGetValue(word, out existingWordAndFrequency);
            if (alreadyExist)
            {
                // Just increase the frequency
                existingWordAndFrequency.Frequency++;
            }
            else
            {
                relevantDictionary.Add(word, new WordAndFrequency()
                {
                    Word = word,
                    Frequency = 1,
                    //FoundInFirstPageTitle = pageTitle
                });
            }
        }

        public void WriteFiles(string keptWordsFilePath, string excludedWordsFilePath)
        {
            // Word we kept in ferquency list
            var lines = WordFrequencies
                .OrderByDescending(wf => wf.Value.Frequency)
                .Select(ent => string.Format("{0}|{1}", ent.Value.Word, ent.Value.Frequency, ent.Value.FoundInFirstPageTitle));
            File.WriteAllLines(keptWordsFilePath, lines);

            // Word we excluded from frequency list
            var lines2 = ExcludedWordFrequencies
                .OrderByDescending(wf => wf.Value.Frequency)
                .Select(ent => string.Format("{0}|{1}", ent.Value.Word, ent.Value.Frequency, ent.Value.FoundInFirstPageTitle));
            File.WriteAllLines(excludedWordsFilePath, lines2);
        }
    }
}
