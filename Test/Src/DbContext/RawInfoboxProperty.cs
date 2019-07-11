using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src.DbContext
{
    public class RawInfoboxProperty
    {
        [Key]
        public int Id { get; set; }
        public string PageTitle { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        /// <summary>
        /// A few pages contain multiple infoboxes hence the need to keep the reference to the infobox ID in the properties
        /// </summary>
        public int InfoboxId { get; set; }
    }
}
