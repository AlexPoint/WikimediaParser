using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryParser.Src.Phrases
{
    public class DefinitionAndExamples
    {
        // Properties -------------

        public string Definition { get; set; }
        public List<string> Examples { get; set; }


        // Methods ----------------

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Definition, string.Join(" | ", this.Examples));
        }
    }
}
