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
        private static List<string> _lastSectionsToFilter = new List<string>() { "See also", "References", "Further reading", "External links"};
        private static readonly Regex LastSectionToFilterRegex = new Regex("\\={2,}\\s*("+ string.Join("|", _lastSectionsToFilter) + ")\\s*\\={2,}", RegexOptions.Compiled);

        private static readonly Regex TitleRegex = new Regex(@"\={2,}\s*([^\=]+)\s*\={2,}", RegexOptions.Compiled);
        /// <summary>
        /// Either [[multilingual]] -> multilingual
        /// OR [[entry|Entries]] -> Entries
        /// OR [[File:Bakunin.png|thumb|upright|Collectivist anarchist [[Mikhail Bakunin]] opposed the [[Marxist]] aim of [[dictatorship of the proletariat]] in favour of universal...]]
        /// </summary>
        private static readonly Regex InterWikiLinkRegex = new Regex(@"\[\[[^\]\[]*(?<=(\||\[))([^\[\]\|]+)\]\]", RegexOptions.Compiled | RegexOptions.Singleline);
        
        /// <summary>
        /// We matches two levels of outbound links:
        /// {{ lorem ipsum }} and {{ lorem {{ ipsum }} }}
        /// Useful in pages such as https://en.wikipedia.org/wiki/Apostolic_succession which contain for instance
        /// references in quote boxes.
        /// </summary>
        private static readonly Regex OutboundLinkRegex = new Regex(@"\{\{(?:(?!(\}\}|\{\{)).)*\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
        //private static readonly Regex OutboundLinkRegex = new Regex(@"\{\{([^\}\{]+|[^\}\{]+\{\{[^\}\{]+\}\}[^\}\{]+)\}\}", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Italic markup is done when using 2 or more '.
        /// Ex: '''Abraham Lincoln'''
        /// </summary>
        private static readonly Regex ItalicMarkup = new Regex(@"'{2,}", RegexOptions.Compiled);

        /// <summary>
        /// Indentation is done with characters such as #, ;, * etc.
        /// Ex: * [[Private (United States)|Private]]
        /// </summary>
        private static readonly Regex IndentationMarkup = new Regex(@"^[#;:\*]+", RegexOptions.Compiled |RegexOptions.Multiline);

        /// <summary>
        /// Ref tags are used to create automatically footnotes in wikipedia articles.
        /// Ex: <ref>Randall (1947), pp. 65–87.</ref>
        /// </summary>
        private static readonly Regex RefTagsContent = new Regex(@"<ref([^>\/]+)?>((?<!\/ref)>|[^>])*<\/ref>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Math tags contains math formula.
        /// Ex: 
        /// </summary>
        private static readonly Regex MathTags = new Regex(@"<math[^>\/]*>(?:(?!<\/math>).)*<\/math>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex GaleryTags = new Regex(@"<gallery[^>\/]*>(?:(?!<\/gallery>).)*<\/gallery>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex SourceTags = new Regex(@"<source[^>\/]*>(?:(?!<\/source>).)*<\/source>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Wikitext contains various tags other than math and ref tags.
        /// </summary>
        private static readonly Regex TagsMarkup = new Regex(@"(<[^>]+>)", RegexOptions.Compiled);

        /// <summary>
        /// Wikitext contains comments between <!-- and -->
        /// </summary>
        private static readonly Regex CommentsMarkup = new Regex(@"<\!--.+-->", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Wikipedia articles contain links to other sites.
        /// Ex: [http://www.illinois.gov/alplm/library/Pages/default.aspx Abraham Lincoln Presidential Library and Museum]
        /// </summary>
        private static readonly Regex WikiUrls = new Regex(@"\[http(s)?:[^\]]+\]", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 
        /// </summary>
        private static readonly Regex WikiTables = new Regex(@"\{\|([^\}\|]|(?<!\|)\}|(?<!\{)\|)*\|\}", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Several articles just redirections to other articles.
        /// Ex: AccessibleComputing -> Computer accessibility (content = "REDIRECT Computer accessibility")
        /// </summary>
        private static readonly Regex RedirectArticles = new Regex(@"^REDIRECT ", RegexOptions.Compiled);
        

        // Constructors -------------------------------

        public WikiMarkupCleaner(){ }

        public WikiMarkupCleaner(List<string> lastSectionsToFilter):this()
        {
            _lastSectionsToFilter = lastSectionsToFilter;
        }


        // Methods ------------------------------------

        /// <summary>
        /// Cleans a wikipedia article text content by:
        /// - decoding the text content received
        /// - removing irrelevant sections
        /// - removing wiki markup
        /// - return empty string on REDIRECT articles
        /// </summary>
        public string CleanArticleContent(string text)
        {
            // HtmlDecode text received and replace linux new lines (decode twice for both XML and HTML escaping)
            text = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(text))
                .Replace("\n", Environment.NewLine);

            // First cleanup sections
            text = CleanupArticleSectionsAfter(text, _lastSectionsToFilter);

            // Then cleanup markup
            text = CleanupMarkup(text);

            // Then filter redirect articles
            if (RedirectArticles.IsMatch(text))
            {
                return string.Empty;
            }

            return text;
        }

        /// <summary>
        /// Cleans the wiki markup from a wikipedia article text content.
        /// </summary>
        private string CleanupMarkup(string text)
        {
            // Cleanup titles
            text = TitleRegex.Replace(text, "");

            // Cleanup tags, comments and tables
            text = RefTagsContent.Replace(text, "");

            var length = text.Length;
            text = OutboundLinkRegex.Replace(text, "");
            while (text.Length < length)
            {
                length = text.Length;
                text = OutboundLinkRegex.Replace(text, "");
            }

            // twice for nested tables
            text = WikiTables.Replace(text, "");
            text = WikiTables.Replace(text, "");
            //
            text = MathTags.Replace(text, "");
            text = GaleryTags.Replace(text, "");
            text = SourceTags.Replace(text, "");
            text = TagsMarkup.Replace(text, "");
            text = CommentsMarkup.Replace(text, "");
            text = WikiUrls.Replace(text, "");
            
            // Cleanup useless bold, italic and indentation markup
            text = ItalicMarkup.Replace(text, "");
            text = IndentationMarkup.Replace(text, "");

            // Cleanup interwiki links (twice for nested links)
            text = FilterInterWikiLinks(text);
            text = FilterInterWikiLinks(text);

            return text;
        }

        private string FilterInterWikiLinks(string text)
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

        /// <summary>
        /// Removes everything in the article after specific sections.
        /// This method is used to remove the last (and irrelevant) sections of an articles such as "See also" or "External links".
        /// </summary>
        private static string CleanupArticleSectionsAfter(string text, List<string> lastSections)
        {
            if (lastSections == null || !lastSections.Any())
            {
                return text;
            }

            var match = LastSectionToFilterRegex.Match(text);
            if (match.Success)
            {
                return text.Substring(0, match.Index);
            }

            return text;
        }
    }
}
