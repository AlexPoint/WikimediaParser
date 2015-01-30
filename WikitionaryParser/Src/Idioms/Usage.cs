using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikitionaryParser.Src.Phrases;

namespace WikitionaryParser.Src.Idioms
{
    [Serializable]
    public class Usage
    {
        public string PartOfSpeech { get; set; }
        public List<DefinitionAndExamples> DefinitionsAndExamples { get; set; }
    }
}
