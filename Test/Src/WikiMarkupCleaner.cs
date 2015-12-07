using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Test.Src
{
    public class WikiMarkupCleaner
    {
        private static readonly Regex TitleRegex = new Regex(@"\={2,}([^\=]+)\={2,}", RegexOptions.Compiled);
        /// <summary>
        /// Either [[multilingual]] -> multilingual
        /// OR [[entry|Entries]] -> Entries
        /// OR [[File:Bakunin.png|thumb|upright|Collectivist anarchist [[Mikhail Bakunin]] opposed the [[Marxist]] aim of [[dictatorship of the proletariat]] in favour of universal...]]
        /// </summary>
        private static readonly Regex InterWikiLinkRegex = new Regex(@"\[\[[^\]\[]*(?<=(\||\[))([^\[\]\|]+)\]\]", RegexOptions.Compiled | RegexOptions.Multiline);
        //private static readonly Regex InterWikiLinkRegex = new Regex(@"\[\[[^\]\[]*(?<=(\||\[))([^\[\]\|]+)\]\]", RegexOptions.Compiled | RegexOptions.Multiline);
        
        /// <summary>
        /// We matches two levels of outbound links:
        /// {{ lorem ipsum }} and {{ lorem {{ ipsum }} }}
        /// Useful in pages such as https://en.wikipedia.org/wiki/Apostolic_succession which contain for instance
        /// references in quote boxes.
        /// </summary>
        private static readonly Regex OutboundLinkRegex = new Regex(@"\{\{([^\}\{]+|[^\}\{]+\{\{[^\}\{]+\}\}[^\}\{]+)\}\}", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ItalicMarkup = new Regex(@"'{2,}", RegexOptions.Compiled);
        private static readonly Regex IndentationMarkup = new Regex(@"^(#|;|:\*|\*)", RegexOptions.Compiled | RegexOptions.Multiline);
        public static readonly Regex RefTagsContent = new Regex(@"<ref([^>\/]+)?>((?<!\/ref)>|[^>])*<\/ref>", RegexOptions.Compiled | RegexOptions.Multiline);
        public static readonly Regex MathTags = new Regex(@"<math>(?:(?!<\/math>).)*<\/math>", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex TagsMarkup = new Regex(@"(&lt;[^&]+&gt;|<[^>]+>)", RegexOptions.Compiled);
        private static readonly Regex CommentsMarkup = new Regex(@"(&lt;|<)\!--.+--(&gt;|>)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex WikiUrls = new Regex(@"\[http:[^\]]+\]", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex WikiTables = new Regex(@"\{\|([^\}\|]|(?<!\|)\}|(?<!\{)\|)*\|\}", RegexOptions.Compiled | RegexOptions.Multiline);
        
        public static string CleanupFullArticle(string text)
        {
            // HtmlDecode text received and replace linux new lines (decode twice for both XML and HTML escaping)
            text = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(text)).Replace("\n", Environment.NewLine);

            // First cleanup sections
            text = CleanupArticleSections(text);

            // Then cleanup markup
            text = CleanupMarkup(text);

            return text;
        }

        private static string CleanupMarkup(string text)
        {
            // Cleanup titles
            text = TitleRegex.Replace(text, "");

            // Cleanup tags, comments and tables
            text = RefTagsContent.Replace(text, "");
            // twice for nested tables
            text = WikiTables.Replace(text, "");
            text = WikiTables.Replace(text, "");
            //
            text = MathTags.Replace(text, "");
            text = TagsMarkup.Replace(text, "");
            text = CommentsMarkup.Replace(text, "");
            text = WikiUrls.Replace(text, "");
            
            // Cleanup useless bold, italic and indentation markup
            text = OutboundLinkRegex.Replace(text, "");
            text = OutboundLinkRegex.Replace(text, "");
            text = ItalicMarkup.Replace(text, "");
            text = IndentationMarkup.Replace(text, "");

            // Cleanup interwiki links (twice for nested links)
            text = FilterInterWikiLinks(text);
            text = FilterInterWikiLinks(text);

            return text;
        }

        private static string FilterInterWikiLinks(string text)
        {
            var matches = InterWikiLinkRegex.Matches(text);
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                if (match.Success && match.Groups.Count > 2)
                {
                    var group = match.Groups[2];
                    text = text.Substring(0, match.Index) + group.Value + text.Substring(match.Index + match.Length);
                }
            }
            
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
