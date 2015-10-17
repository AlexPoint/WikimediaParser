using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WikitionaryDumpParser.Src
{
    /// <summary>
    /// Reader for wikimedia SQL dump files.
    /// Those files are huge INSERT INTO queries.
    /// </summary>
    public class SqlDumpFileReader: IDisposable
    {
        private const char SplitCharacter = ')';

        // Properties -------------------------------------

        private readonly StreamReader _reader;
        

        // Constructors -----------------------------------

        public SqlDumpFileReader(string filePath)
        {
            Stream fileStream = File.OpenRead(filePath);
            Stream decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress);
            _reader = new StreamReader(decompressedStream);
        }


        // Methods ----------------------------------------

        public PageInfo ReadNext()
        {
            var line = new StringBuilder();
            while (!_reader.EndOfStream)
            {
                // Read characters until finding the split character
                var nextChar = (char) _reader.Read();
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

        
        private const string PageIdPattern = @"\((\d+)\,\d+\,\'([^\']+)\'\,[^\)]+\)";
        private static readonly Regex PageIdRegex = new Regex(PageIdPattern, RegexOptions.Compiled);

        /// <summary>
        /// Extract page info from a dump file line.
        /// Ex: (64578,0,'History_of_Iceland','',278,0,0,0.190001439480112,'20150902131138','20150902131138',679102147,47981,'wikitext')
        /// </summary>
        /// <param name="line">A dump file "line" (ie, a value inserted in db)</param>
        /// <returns>The parsed page info if successful, null otherwise</returns>
        private PageInfo ExtractPageInfo(string line)
        {
            var match = PageIdRegex.Match(line);
            if (match.Success)
            {
                var id = int.Parse(match.Groups[1].Value);
                var title = match.Groups[2].Value;
                return new PageInfo
                {
                    Id = id,
                    StoredTitle = title
                };
            }
            
            return null;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
