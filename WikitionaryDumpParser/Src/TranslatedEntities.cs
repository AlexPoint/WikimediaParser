using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class TranslatedEntities
    {
        // Properties ----------------------------

        public string SrcLanguage { get; set; }
        public string TgtLanguage { get; set; }
        public List<TranslatedEntity>Entities { get; set; }


        // Methods ------------------------------

        public IEnumerable<string> GetTextFileLines()
        {
            // First line contains the languages
            var lines = new List<string>()
            {
                string.Format("{0}->{1}", SrcLanguage, TgtLanguage)
            };
            // Following lines are the entities
            return lines.Union(Entities.Select(ent => ent.ToString()));
        }
    }
}
