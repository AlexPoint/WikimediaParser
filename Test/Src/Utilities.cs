using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenNLP.Tools.SentenceDetect;
using OpenNLP.Tools.Tokenize;

namespace Test.Src
{
    public static class Utilities
    {
        public static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";
        public static readonly string PathToDownloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wikimedia\\Downloads\\";

        public static string LowerCaseFirstLetter(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return null;
            }
            return char.ToLower(word[0]) + word.Substring(1);
        }
        public static Word LowerCaseFirstLetter(Word word)
        {
            if (word == null || string.IsNullOrEmpty(word.Token))
            {
                return null;
            }
            return WordDictionary.GetOrCreate(char.ToLower(word.Token[0]) + word.Token.Substring(1));
        }

        /// <summary>
        /// Replace all invalid filename characters by '_'
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            var sb = new StringBuilder();
            foreach (var c in fileName)
            {
                sb.Append(Path.GetInvalidFileNameChars().Contains(c) ? '_' : c);
            }
            return sb.ToString();
        }

        public static void ExtractTokensFromTxtFiles(Func<Word[], int, bool> tokensProcessor, int nbOfSentencesToParse,
            int nbOfSentencesToSkip = 0)
        {
            var relevantDirectories = Directory.GetDirectories(PathToDownloadDirectory)
                .Where(dir => Regex.IsMatch(dir, "enwiki-latest-pages-meta-current"));
            var sentenceDetector = new EnglishMaximumEntropySentenceDetector(PathToProject + "Data/EnglishSD.nbin");
            var tokenizer = new EnglishRuleBasedTokenizer(false);

            var sentenceCounter = 0;
            foreach (var directory in relevantDirectories.OrderBy(d => d)) // ordering is important here to be able to relaunch the parsing from anywhere
            {
                var txtFiles = Directory.GetFiles(directory);
                foreach (var txtFile in txtFiles.OrderBy(f => f)) // ordering is important here to be able to relaunch the parsing from anywhere
                {
                    var sentences = File.ReadAllLines(txtFile)
                        .Where(l => !string.IsNullOrEmpty(l))
                        .SelectMany(l => sentenceDetector.SentenceDetect(l))
                        .ToList();
                    foreach (var sentence in sentences)
                    {
                        // Increase counter
                        sentenceCounter++;
                        if (sentenceCounter > nbOfSentencesToParse)
                        {
                            return;
                        }
                        if (sentenceCounter <= nbOfSentencesToSkip)
                        {
                            continue;
                        }

                        var tokens = tokenizer.Tokenize(sentence)
                            .Select(WordDictionary.GetOrCreate) // intern strings to avoid huge consumption of memory
                            .ToArray();
                        var success = tokensProcessor(tokens, sentenceCounter);
                    }
                }
                Console.WriteLine("Done parsing sentences in directory: '{0}'", directory);
            }
        }
    }
}
