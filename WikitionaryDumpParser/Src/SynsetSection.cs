using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class SynsetSection
    {
        // Properties --------------------------

        public string Synset { get; set; }
        public int Index { get; set; }

        // Ctors -------------------------------
        public SynsetSection(Match match)
        {
            this.Index = match.Index;
            var parts = match.Value.Split(new[] {'{', '}', '|'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
	        {
		        this.Synset = parts[1]; 
	        }
        }
    }
}
