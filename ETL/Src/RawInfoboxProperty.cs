using System;
using System.Collections.Generic;
using System.Text;

namespace ETL.Src
{
    class RawInfoboxProperty
    {
        public int ID { get; set; }
        public string PageTitle { get; set; }
        public string InfoboxId { get; set; }
        /// <summary>
        /// The keywod 'Key' is reserved in SQL hence the use of 'PropKey' instead
        /// </summary>
        public string PropKey { get; set; }
        /// <summary>
        /// The keywod 'Value' is reserved in SQL hence the use of 'PropValue' instead
        /// </summary>
        public string PropValue { get; set; }
    }
}
