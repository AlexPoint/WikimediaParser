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
        private static readonly Regex StartInterWikiLinkRegex = new Regex(@"\[\[((?=[^\|\]]+\]\])|[^\|\]]+\|(?=[^\|\]]+\]\]))", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex EndInterWikiLinkRegex = new Regex(@"\]\]", RegexOptions.Compiled);
        /// <summary>
        /// We matches two levels of outbound links:
        /// {{ lorem ipsum }} and {{ lorem {{ ipsum }} }}
        /// Useful in pages such as https://en.wikipedia.org/wiki/Apostolic_succession which contain for instance
        /// references in quote boxes.
        /// </summary>
        private static readonly Regex OutboundLinkRegex = new Regex(@"\{\{([^\}\{]+|[^\}\{]+\{\{[^\}\{]+\}\}[^\}\{]+)\}\}", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ItalicMarkup = new Regex(@"'{2,}", RegexOptions.Compiled);
        private static readonly Regex IndentationMarkup = new Regex(@"^(#|;|:\*|\*)", RegexOptions.Compiled | RegexOptions.Multiline);
        public static readonly Regex RefTagsContent = new Regex(@"<ref([^>]+)?>(?:(?!<\/ref>).)*<\/ref>", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex TagsMarkup = new Regex(@"(&lt;[^&]+&gt;|<[^>]+>)", RegexOptions.Compiled);
        private static readonly Regex CommentsMarkup = new Regex(@"(&lt;|<)\!--.+--(&gt;|>)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex WikiUrls = new Regex(@"\[http:[^\)]+\]", RegexOptions.Compiled | RegexOptions.Multiline);



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

            // Cleanup tags & comments
            text = RefTagsContent.Replace(text, " ");
            text = TagsMarkup.Replace(text, " ");
            text = CommentsMarkup.Replace(text, " ");
            text = WikiUrls.Replace(text, " ");

            // Cleanup useless bold, italic and indentation markup
            text = OutboundLinkRegex.Replace(text, "");
            text = ItalicMarkup.Replace(text, "");
            text = IndentationMarkup.Replace(text, "");
            
            // Cleanup interwiki links
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
                    if (title.Contains("See also") || title.Contains("References") || title.Contains("Further reading") || title.Contains("External links"))
                    {
                        var removedText = text.Substring(match.Index);
                        //Console.WriteLine("Removed section:");
                        //Console.WriteLine(removedText);
                        return text.Substring(0, match.Index);
                    }
                }
            }

            return text;
        }
    }
}
