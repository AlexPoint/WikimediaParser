using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public class TranslatedEntity
    {
        // Properties --------------------------------------

        public string SrcName { get; set; }
        public string TgtName { get; set; }
        public string Definition { get; set; }
        public string Pos { get; set; }

        // Methods -----------------------------------------

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}|{3}", this.SrcName,
                this.TgtName, this.Definition, this.Pos);
        }
    }
}
