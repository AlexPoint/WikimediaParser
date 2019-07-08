using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public class RawDumpParsedInfobox
    {
        [Key]
        public int Id { get; set; }
        public string PageTitle { get; set; }
        public string Markdown { get; set; }
    }

    public class ParsedInfobox
    {
        [Key]
        public int Id { get; set; }
        public string ArticleUrl { get; set; }
        public string Html { get; set; }
        public List<InfoboxProperty> Properties { get; set; }
    }

    public class InfoboxProperty
    {
        [Key]
        public int Id { get; set; }
        public string Property { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Property, Value);
        }
    }
}

