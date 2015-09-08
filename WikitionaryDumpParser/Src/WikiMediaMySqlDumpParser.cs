using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class WikiMediaMySqlDumpParser
    {

        public void ParsePageIds(string sqlDumpFilePath, string outputFilePath)
        {
            var pageAndIds = new List<Tuple<int, string>>();
            var lastProgressLog = 0L;

            // Split on ')' characters 
            const char splitCharacter = ')';

            using (var fStream = File.OpenRead(sqlDumpFilePath))
            {
                using (var reader = new StreamReader(fStream))
                {
                    StringBuilder line = new StringBuilder();
                    while (!reader.EndOfStream)
                    {
                        var nextChar = (char)reader.Read();
                        line.Append(nextChar);
                        if (nextChar == splitCharacter)
                        {
                            // Try to extract the page id and name
                            var pageAndId = ExtractPageAndId(line.ToString());
                            if (pageAndId != null)
                            {
                                pageAndIds.Add(pageAndId);
                                if (pageAndIds.Count == 1000)
                                {
                                    // Write lines (by batch)
                                    var lines = pageAndIds
                                        .Select(tup => string.Format("{0}|{1}", tup.Item1, tup.Item2))
                                        .ToList();
                                    File.AppendAllLines(outputFilePath, lines);
                                    pageAndIds.Clear();

                                    // Show progress in console
                                    var progress = (fStream.Position * 100) / fStream.Length;
                                    if (lastProgressLog < progress)
                                    {
                                        Console.WriteLine("{0}%", progress);
                                        lastProgressLog = progress;
                                    }
                                }
                            }

                            // Clear the string builder
                            line.Clear();
                        }
                    }
                }
            }

            // Write the remaining page ids in "cache"
            if (pageAndIds.Any())
            {
                var lines = pageAndIds
                        .Select(tup => string.Format("{0}|{1}", tup.Item1, tup.Item2))
                        .ToList();
                File.AppendAllLines(outputFilePath, lines);
                pageAndIds.Clear();
            }
        }

        public void ParseLanguageLinks(string sqlDumpFilePath, string outputFilePath)
        {
            var languageLinks = new List<Tuple<int, string, string>>();
            var lastProgressLog = 0L;

            // Split on ')' characters 
            const char splitCharacter = ')';

            using (var fStream = File.OpenRead(sqlDumpFilePath))
            {
                using (var reader = new StreamReader(fStream))
                {
                    StringBuilder line = new StringBuilder();
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
                                if (languageLinks.Count == 1000)
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
                                }
                            }

                            // Clear the string builder
                            line.Clear();
                        }
                    }
                }
            }

            // Write the remaining page ids in "cache"
            if (languageLinks.Any())
            {
                var lines = languageLinks
                        .Select(tup => string.Format("{0}|{1}", tup.Item1, tup.Item2))
                        .ToList();
                File.AppendAllLines(outputFilePath, lines);
                languageLinks.Clear();
            }
        }

        private const string PageIdPattern = @"\((\d+)\,\'defaultsort\'\,\'(.+)\'(,NULL)?\)";
        private static readonly Regex PageIdRegex = new Regex(PageIdPattern, RegexOptions.Compiled);

        private Tuple<int, string> ExtractPageAndId(string line)
        {
            var match = PageIdRegex.Match(line);
            if (match.Success)
            {
                var id = int.Parse(match.Groups[1].Value);
                var page = match.Groups[2].Value;
                //Console.WriteLine("#{0} -> {1}", id, page);
                return new Tuple<int, string>(id, page);
            }
            else
            {
                return null;
            }
        }

        private const string LanguageLinkPattern = @"\((\d+)\,\'(fr)\'\,\'(.+)\'\)";
        private static readonly Regex LanguageLinkRegex = new Regex(LanguageLinkPattern, RegexOptions.Compiled);

        private Tuple<int, string, string> ExtractLanguageLink(string line)
        {
            //(34778507,'es','Categoría:Futbolistas del Arlesey Town Football Club')
            var match = LanguageLinkRegex.Match(line);
            if (match.Success)
            {
                var pageId = int.Parse(match.Groups[1].Value);
                var lang = match.Groups[2].Value;
                var value = match.Groups[3].Value;
                //Console.WriteLine("#{0} -> {1}", id, page);
                return new Tuple<int, string, string>(pageId, lang, value);
            }
            else
            {
                return null;
            }
        }
    }
}
