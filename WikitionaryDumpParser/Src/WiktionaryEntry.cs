using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class WiktionaryEntry
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public List<PosTranslations> PosTranslations { get; set; }
    }

    public class PosTranslations
    {
        // Noun/Verb...
        public string Pos { get; set; }
        public List<SynsetTranslation> SynsetTranslations { get; set; }
    }

    public class SynsetTranslation
    {
        public string Definition { get; set; }
        public List<Translation> Translations { get; set; }
    }

    public class Translation
    {
        public string Name { get; set; }
        public string Language { get; set; }
    }
}
