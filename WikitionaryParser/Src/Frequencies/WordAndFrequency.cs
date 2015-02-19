using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryParser.Src.Frequencies
{
    public class WordAndFrequency
    {
        public string Word { get; set; }
        public float Frequency { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Word, Frequency);
        }
    }
}
