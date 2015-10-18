using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNLP.Tools.Ling;

namespace Test.Src
{
    public class FrequencyResults
    {

        private Dictionary<string, WordAndFrequency> WordFrequencies = new Dictionary<string, WordAndFrequency>();


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
            WordAndFrequency existingWordAndFrequency;
            var alreadyExist = WordFrequencies.TryGetValue(word, out existingWordAndFrequency);
            if (alreadyExist)
            {
                // Just increase the frequency
                existingWordAndFrequency.Frequency++;
            }
            else
            {
                WordFrequencies.Add(word, new WordAndFrequency()
                {
                    Word = word,
                    Frequency = 1,
                    FoundInFirstPageTitle = pageTitle
                });
            }

            //return !alreadyExist;
        }

        public void WriteInFile(string filePath)
        {
            var lines = WordFrequencies
                .OrderByDescending(wf => wf.Value.Frequency)
                .Select(ent => string.Format("{0}|{1}|{2}", ent.Value.Word, ent.Value.Frequency, ent.Value.FoundInFirstPageTitle));
            File.WriteAllLines(filePath, lines);
        }
    }
}
