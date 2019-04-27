using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class WikiPage
    {
        public string Title { get; set; }
        public int Id { get; set; }
        public int Ns { get; set; }
        public string Revision { get; set; }
        public string Text { get; set; }


        // Public method --------------------------------------

        public List<string> GetInfoboxTexts()
        {
            var infoboxes = new List<string>();

            var infoboxRegex = new Regex("{{Infobox", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            var matches = infoboxRegex.Matches(Text);
            for(var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var initialPosition = match.Index;
                var countOpeningBrackets = 0;
                var currentPosition = initialPosition;
                do
                {
                    var character = Text[currentPosition];
                    if (character == '{')
                    {
                        countOpeningBrackets++;
                    }
                    else if (character == '}')
                    {
                        countOpeningBrackets--;
                    }

                    currentPosition++;
                } while (countOpeningBrackets > 0 & currentPosition < Text.Length);

                infoboxes.Add(Text.Substring(initialPosition, (currentPosition - initialPosition)));
            }

            return infoboxes;
        }

    }
}
