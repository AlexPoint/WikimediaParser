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
    }
}
