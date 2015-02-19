using System;
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

        private static readonly List<string> EnglishStopWords = new List<string>() { "a", "about", "above", "after", "again", "against", "all", "am", "an", "and", "any", "are", "aren't", "as", "at", "be", "because", "been", "before", "being", "below", "between", "both", "but", "by", "can't", "cannot", "could", "couldn't", "did", "didn't", "do", "does", "doesn't", "doing", "don't", "down", "during", "each", "few", "for", "from", "further", "had", "hadn't", "has", "hasn't", "have", "haven't", "having", "he", "he'd", "he'll", "he's", "her", "here", "here's", "hers", "herself", "him", "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", "i've", "if", "in", "into", "is", "isn't", "it", "it's", "its", "itself", "let's", "me", "more", "most", "mustn't", "my", "myself", "no", "nor", "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our", "ours", "ourselves", "out", "over", "own", "same", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", "so", "some", "such", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then", "there", "there's", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those", "through", "to", "too", "under", "until", "up", "very", "was", "wasn't", "we", "we'd", "we'll", "we're", "we've", "were", "weren't", "what", "what's", "when", "when's", "where", "where's", "which", "while", "who", "who's", "whom", "why", "why's", "with", "won't", "would", "wouldn't", "you", "you'd", "you'll", "you're", "you've", "your", "yours", "yourself", "yourselves" };

        static void Main(string[] args)
        {
            var wordFrequencyParser = new WordFrequencyParser();
            var wordFrequencies = wordFrequencyParser.ParseWordFrequencies(WordFrequencyParser.FrequencyListType.TvShows);

            Console.WriteLine("{0} words parsed", wordFrequencies.Count);
            var duplicates = wordFrequencies.GroupBy(wf => wf.Word)
                .Where(grp => grp.Count() > 2)
                .ToList();
            Console.WriteLine("Duplicates:");
            foreach (var duplicate in duplicates)
            {
                Console.WriteLine("{0}", string.Join(", ", duplicate));
            }
            /*foreach (var wordAndFrequency in wordFrequencies)
            {
                Console.WriteLine("{0} -> {1}", wordAndFrequency.Word, wordAndFrequency.Frequency);
            }*/


            /*var idiomParser = new EnglishIdiomsParser();
            var idiom = idiomParser.ParseIdiomPage("/wiki/out_of_the_box");
            idiom.Print();*/

            /*var allIdioms = DeserializeIdioms(PathToSerializedIdioms);

            var nonPhrasalVerbIdioms = allIdioms.Where(id => !id.Categories.Contains("English phrasal verbs")).ToList();
            var idiomsWithMoreThan1Word = nonPhrasalVerbIdioms.Where(id => id.Name.Split(' ').Where(s => !EnglishStopWords.Contains(s)).Count() > 1).ToList();

            foreach (var idiom in idiomsWithMoreThan1Word)
            {
                Console.WriteLine(idiom.Name);
            }*/
            
            /*foreach (var idiom in idiomsWithMoreThan1Word.Where(id => id.Usages.All(u => u.DefinitionsAndExamples.Any(d => d.Definition.StartsWith("Used other than as")))))
            {
                Console.WriteLine(idiom.Name);
            }*/
            

            /*foreach (var group in allIdioms.GroupBy(id => id.Name.Split(' ').Count()).OrderBy(g => g.Key))
            {
                Console.WriteLine("{0} words -> {1} idioms", group.Key, group.Count());
                foreach (var idiom in group)
                {
                    Console.WriteLine(idiom.Name);
                }
            }*/

            /*foreach (var category in allIdioms.SelectMany(i => i.Categories).Distinct())
            {
                var nbOfIdioms = allIdioms.Count(id => id.Categories.Contains(category));
                Console.WriteLine("{0} - {1}", category, nbOfIdioms);
            }*/

            /*var categories = new List<string>() { "English interjections", "English dated terms", "English informal terms", "English colloquialisms", "English slang" };
            foreach (var category in categories)
            {
                Console.WriteLine(category + ":");
                var relevantIdioms = idiomsWithMoreThan1Word.Where(id => id.Categories.Contains(category));
                foreach (var idiom in relevantIdioms)
                {
                    Console.WriteLine(idiom.Name);
                }
                Console.WriteLine("=============");
            }*/

            /*Console.WriteLine("All categories:");
            foreach (var category in allIdioms.SelectMany(i => i.Categories).Distinct())
            {
                Console.WriteLine(category);
            }*/

            /*// Parse idioms on wikitionary
            var parser = new WikitionaryParser.Src.WikitionaryParser();
            var idioms = parser.ParseAllEnglishIdioms();

            SerializeIdioms(idioms, PathToSerializedIdioms);

            Console.WriteLine("{0} idioms parsed:", idioms.Count);
            foreach (var proverb in idioms)
            {
                proverb.Print();
                Console.WriteLine("-------------");
            }*/

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
