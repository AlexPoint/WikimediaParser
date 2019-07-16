using ALE.ETLBox.DataFlow;
using System;
using System.Collections.Generic;
using System.Text;

namespace ETL.Src
{
    class WikiCompanyData
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Revenue { get; set; }
        [ColumnMap("revenue_year")]
        public string RevenueYear { get; set; }
    }
}
