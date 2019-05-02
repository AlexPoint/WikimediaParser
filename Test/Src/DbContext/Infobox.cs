using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src.DbContext
{
    public class Infobox
    {
        [Key]
        public int Id { get; set; }
        public string PageTitle { get; set; }
        public int PageId { get; set; }

        /// <summary>
        /// Infobox markup text as parsed from Wikipedia dump files
        /// </summary>
        public string RawText { get; set; }

        /// <summary>
        /// The template of the current infobox.
        /// For instance, if the markup is "{{Infobox company | ... }}", the template will be "company"
        /// </summary>
        public string Template { get; set; }
        public List<InfoboxProperty> Properties { get; set; }


    }
}
