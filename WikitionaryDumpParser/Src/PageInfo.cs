using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    /// <summary>
    /// The id and title of a wikimedia page.
    /// </summary>
    public class PageInfo
    {

        // Properties -------------------------------

        public int Id { get; set; }
        public string StoredTitle { get; set; }


        // Methods ----------------------------------

        private static readonly Dictionary<char, string> CharactersToReplace = new Dictionary<char, string>()
        {
            {'_', " "},
            {'&', " and "},
            {'\\', ""}
        };

        public string GetDisplayedTitle()
        {
            if (string.IsNullOrEmpty(this.StoredTitle))
            {
                return "";
            }

            var displayedTitle = new StringBuilder();
            foreach(var c in StoredTitle)
            {
                if (CharactersToReplace.ContainsKey(c))
                {
                    displayedTitle.Append(CharactersToReplace[c]);
                }
                else
                {
                    displayedTitle.Append(c);
                }
            }

            return displayedTitle.ToString();
        }
    }
}
