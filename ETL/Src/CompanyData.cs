using System;
using System.Collections.Generic;
using System.Text;

namespace ETL.Src
{
    class CompanyData
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Industry { get; set; }
        public string Country { get; set; }
        public decimal Revenue_2018_mUSD { get; set; }
        public decimal MarketValue_2018_mUSD { get; set; }
        public string Headquarters { get; set; }
        public string Ceo_2018 { get; set; }
        public decimal Profits_2018_mUSD { get; set; }
        public decimal Assets_2018_mUSD { get; set; }
    }
}
