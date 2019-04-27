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
        public string RawText { get; set; }
    }
}
