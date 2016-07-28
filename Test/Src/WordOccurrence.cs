using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public class WordOccurrence
    {
        public string Word { get; set; }
        public bool IsFirstTokenInSentence { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Word, this.IsFirstTokenInSentence);
        }
    }

    public class WordOccurrenceAndFrequency: WordOccurrence
    {
        public long Frequency { get; set; }
    }
}
