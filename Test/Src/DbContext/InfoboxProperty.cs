using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src.DbContext
{
    public class InfoboxProperty
    {
        [Key]
        public int Id { get; set; }
        public string RawText { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public virtual Infobox Infobox { get; set; }
    }
}
