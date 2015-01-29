using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryParser.Src
{
    /// <summary>
    /// An entity parsed on wikitionary
    /// </summary>
    public class WikitionaryEntity
    {
        public string SourceRelativeUrl { get; set; }

        /// <summary>
        /// The absolute url of the page where we parsed this entity
        /// </summary>
        public string GetSourceAbsoluteUrl()
        {
            return WikitionaryParser.WikitionaryRootUrl + SourceRelativeUrl;
        }
    }
}
