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
    public class DumpFileReader
    {
        private readonly StreamReader reader;
        private const char SplitCharacter = ')';

        public DumpFileReader(string filePath)
        {
            using (var fStream = File.OpenRead(filePath))
            {
                using (var decompressedStream = new GZipStream(fStream, CompressionMode.Decompress))
                {
                    reader = new StreamReader(decompressedStream);
                }
            }
        }

        public PageInfo ReadNext()
        {
            var line = new StringBuilder();
            while (!reader.EndOfStream)
            {
                // Read characters until finding the split character
                var nextChar = (char) reader.Read();
                line.Append(nextChar);
                if (nextChar == SplitCharacter)
                {
                    // Try to extract the page id and name
                    var pageInfo = ExtractPageInfo(line.ToString());
                    if (pageInfo != null)
                    {
                        return pageInfo;
                    }

                    // Clear the string builder
                    line.Clear();
                }
            }

            // Try to extract the page info even if we didn't encounter the split character at the end
            if (!string.IsNullOrEmpty(line.ToString()))
            {
                var pageInfo = ExtractPageInfo(line.ToString());
                return pageInfo;
            }

            // End of stream is reached and there is no page info left to find
            return null;
        }


        // A wikiepdia page inset looks like
        // (64578,0,'History_of_Iceland','',278,0,0,0.190001439480112,'20150902131138','20150902131138',679102147,47981,'wikitext')
        private const string PageIdPattern = @"\((\d+)\,\d+\,\'([^\']+)\'\,[^\)]+\)";
        private static readonly Regex PageIdRegex = new Regex(PageIdPattern, RegexOptions.Compiled);

        private PageInfo ExtractPageInfo(string line)
        {
            var match = PageIdRegex.Match(line);
            if (match.Success)
            {
                var id = int.Parse(match.Groups[1].Value);
                var title = match.Groups[2].Value;
                var displayedTitle = title.Replace("_", " ").Replace("&", " and ");
                //Console.WriteLine("#{0} -> {1}", id, page);
                return new PageInfo()
                {
                    Id = id,
                    Title = displayedTitle
                };
            }
            
            return null;
        }
    }
}
