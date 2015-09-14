using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class TranslatedEntities
    {
        public string SrcLanguage { get; set; }
        public string TgtLanguage { get; set; }
        public List<TranslatedEntity>Entities { get; set; }
    }
}
