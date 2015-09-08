using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class TextSection
    {
        // Properties ---------------------------

        public int StartIndex { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }


        // Ctors --------------------------------
        public TextSection(Match sectionMatch)
        {
            this.StartIndex = sectionMatch.Index;
            this.Level = sectionMatch.Value.Count(c => c == '=')/2;
            this.Name = sectionMatch.Value.Trim(new char[] {'='});
        }
    }
}
