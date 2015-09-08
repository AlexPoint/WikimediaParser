using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class TranslationEntry
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public string Synset { get; set; }
        public string Pos { get; set; }
    }
}
