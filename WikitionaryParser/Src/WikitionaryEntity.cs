using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryParser.Src
{
    public class WikitionaryEntity
    {
        public string SourceRelativeUrl { get; set; }

        public string GetSourceAbsoluteUrl()
        {
            return WikitionaryParser.WikitionaryRootUrl + SourceRelativeUrl;
        }
    }
}
