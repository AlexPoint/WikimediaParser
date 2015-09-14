using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class WikiMediaMySqlDumpParser
    {

        
        public List<LanguageLink> ParseLanguageLinks(string sqlDumpFilePath)
        {
            var languageLinks = new List<LanguageLink>();
            //var lastProgressLog = 0L;

            // Split on ')' characters 
            const char splitCharacter = ')';

            // Open the file
            using (var fStream = File.OpenRead(sqlDumpFilePath))
            {
                // Decompress the stream
                using (var decompressedStream = new GZipStream(fStream, CompressionMode.Decompress))
                {
                    // Read the decompressed stream
                    using (var reader = new StreamReader(decompressedStream))
                    {
                        var line = new StringBuilder();
                        while (!reader.EndOfStream)
                        {
                            var nextChar = (char)reader.Read();
                            line.Append(nextChar);
                            if (nextChar == splitCharacter)
                            {
                                // Try to extract the page id and name
                                var languageLink = ExtractLanguageLink(line.ToString());
                                if (languageLink != null)
                                {
                                    languageLinks.Add(languageLink);
                                    /*if (languageLinks.Count == 1000)
                                    {
                                        // Write lines (by batch)
                                        var lines = languageLinks
                                            .Select(tup => string.Format("{0}|{1}|{2}", tup.Item1, tup.Item2, tup.Item3))
                                            .ToList();
                                        File.AppendAllLines(outputFilePath, lines);
                                        languageLinks.Clear();

                                        // Show progress in console
                                        var progress = (fStream.Position * 100) / fStream.Length;
                                        if (lastProgressLog < progress)
                                        {
                                            Console.WriteLine("{0}%", progress);
                                            lastProgressLog = progress;
                                        }
                                    }*/
                                }

                                // Clear the string builder
                                line.Clear();
                            }
                        }
                    }
                }
            }

            // Write the remaining page ids in "cache"
            /*if (languageLinks.Any())
            {
                var lines = languageLinks
                        .Select(tup => string.Format("{0}|{1}", tup.Item1, tup.Item2))
                        .ToList();
                File.AppendAllLines(outputFilePath, lines);
                languageLinks.Clear();
            }*/
            return languageLinks;
        }


        private const string LanguageLinkPattern = @"\((\d+)\,\'(fr)\'\,\'(.+)\'\)";
        private static readonly Regex LanguageLinkRegex = new Regex(LanguageLinkPattern, RegexOptions.Compiled);


        private LanguageLink ExtractLanguageLink(string line)
        {
            //(34778507,'es','Categoría:Futbolistas del Arlesey Town Football Club')
            var match = LanguageLinkRegex.Match(line);
            if (match.Success)
            {
                var pageId = int.Parse(match.Groups[1].Value);
                var lang = match.Groups[2].Value;
                var title = match.Groups[3].Value;
                return new LanguageLink
                {
                    PageId = pageId,
                    LanguageCode = lang,
                    Title = title
                };
            }

            return null;
        }
    }
}
