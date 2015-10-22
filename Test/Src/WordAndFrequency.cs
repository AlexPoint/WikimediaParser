using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public class WordAndFrequency
    {
        public string Word { get; set; }
        public long Frequency { get; set; }
        public bool IsFirstLineToken { get; set; }
        public string FoundInFirstPageTitle { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Word, this.Frequency);
        }
    }
}
