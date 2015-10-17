using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Test.Src
{
    public class WikiMarkupCleaner
    {
        private static readonly Regex TitleRegex = new Regex(@"\={2,}([^\=]+)\={2,}", RegexOptions.Compiled);
        /// <summary>
        /// Either [[multilingual]] -> multilingual
        /// OR [[entry|Entries]] -> Entries
        /// </summary>
        private static readonly Regex StartInterWikiLinkRegex = new Regex(@"\[\[((?=[^\|\]]+\]\])|[^\|\]]+\|(?=[^\|\]]+\]\]))", RegexOptions.Compiled);
        private static readonly Regex EndInterWikiLinkRegex = new Regex(@"\]\]", RegexOptions.Compiled);
        private static readonly Regex OutboundLinkRegex = new Regex(@"\{\{[^\}]+\}\}", RegexOptions.Compiled);
        private static readonly Regex ItalicMarkup = new Regex(@"'{2,}", RegexOptions.Compiled);
        private static readonly Regex IndentationMarkup = new Regex(@"^(#|;|:\*|\*)", RegexOptions.Compiled | RegexOptions.Multiline);


        public static List<string> CleanupFullArticle(string text)
        {
            // First cleanup sections
            text = CleanupArticleSections(text);

            // Then cleanup markup
            text = CleanupMarkup(text);

            // Finally split the different lines
            var cleanedLines = text.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList();
            return cleanedLines;
        }

        private static string CleanupMarkup(string text)
        {
            // Cleanup titles
            text = TitleRegex.Replace(text, "");

            // Cleanup useless markup
            text = OutboundLinkRegex.Replace(text, "");
            text = ItalicMarkup.Replace(text, "");
            text = IndentationMarkup.Replace(text, "");

            // 
            text = StartInterWikiLinkRegex.Replace(text, "");
            text = EndInterWikiLinkRegex.Replace(text, "");

            return text;
        }

        private static string CleanupArticleSections(string text)
        {
            var matches = TitleRegex.Matches(text);
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                if (match.Success && match.Groups.Count > 1)
                {
                    var title = match.Groups[1].Value;
                    if (title.Contains("See also"))
                    {
                        var removedText = text.Substring(match.Index);
                        Console.WriteLine("Removed section:");
                        Console.WriteLine(removedText);
                        return text.Substring(0, match.Index);
                    }
                }
            }

            return text;
        }
    }
}
