﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using WikitionaryParser.Src.Frequencies;
using WikitionaryParser.Src.Idioms;
using WikitionaryParser.Src.Phrases;

namespace Test
{
    class Program
    {
        private static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";
        private static readonly string PathToSerializedIdioms = PathToProject + "Data/idioms.nbin";
        private static readonly string PathToWiktionaryPages = PathToProject + "Data/enwiktionary-20150901-pages-meta-current.xml";

        //private static readonly List<string> EnglishStopWords = new List<string>() { "a", "about", "above", "after", "again", "against", "all", "am", "an", "and", "any", "are", "aren't", "as", "at", "be", "because", "been", "before", "being", "below", "between", "both", "but", "by", "can't", "cannot", "could", "couldn't", "did", "didn't", "do", "does", "doesn't", "doing", "don't", "down", "during", "each", "few", "for", "from", "further", "had", "hadn't", "has", "hasn't", "have", "haven't", "having", "he", "he'd", "he'll", "he's", "her", "here", "here's", "hers", "herself", "him", "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", "i've", "if", "in", "into", "is", "isn't", "it", "it's", "its", "itself", "let's", "me", "more", "most", "mustn't", "my", "myself", "no", "nor", "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our", "ours", "ourselves", "out", "over", "own", "same", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", "so", "some", "such", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then", "there", "there's", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those", "through", "to", "too", "under", "until", "up", "very", "was", "wasn't", "we", "we'd", "we'll", "we're", "we've", "were", "weren't", "what", "what's", "when", "when's", "where", "where's", "which", "while", "who", "who's", "whom", "why", "why's", "with", "won't", "would", "wouldn't", "you", "you'd", "you'll", "you're", "you've", "your", "yours", "yourself", "yourselves" };

        static void Main(string[] args)
        {
            var srcLanguage = "en";
            var tgtLanguages = new List<string>() {"fr"};
            var outputFilePath = PathToProject + "Output/en-fr-dictionary.txt";

            var dumpParser = new WiktionaryDumpParser.Src.WiktionaryDumpParser();
            var entries = dumpParser.ParseDumpFile(PathToWiktionaryPages, srcLanguage, tgtLanguages, outputFilePath);



            /*var posTranslations = translationEntries
                .GroupBy(ent => ent.Pos)
                .Select(grp => new PosTranslations()
                {
                    Pos = grp.Key,
                    SynsetTranslations = grp.GroupBy(ent => ent.Synset)
                        .Select(synGrp => new SynsetTranslation()
                        {
                            Definition = synGrp.Key,
                            Translations = synGrp.Select(ent => new Translation()
                            {
                                Language = ent.Language,
                                Name = ent.Name
                            })
                            .ToList()
                        })
                        .ToList()
                })
                .ToList();
            return posTranslations;*/

            Console.WriteLine("Parsed {0} {1} entries ({2} distinct)", entries.Count, srcLanguage, entries.Select(ent => ent.Name).Distinct());

            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }

        private static void SerializeIdioms(List<Idiom> idioms, string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Create))
            {
                var bin = new BinaryFormatter();
                bin.Serialize(stream, idioms);
            }
        }

        private static List<Idiom> DeserializeIdioms(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var bin = new BinaryFormatter();
                var idioms = (List<Idiom>)bin.Deserialize(stream);
                return idioms;
            }
        }
    }
}
