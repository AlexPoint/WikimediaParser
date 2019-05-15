using System;
using System.Collections.Generic;
using System.Text;

namespace CompanyScrapper.Src.Forbes
{
    /// <summary>
    /// {position":1574,"rank":1574,"name":"360 Security Technology","uri":"360-security-technology",
    /// "imageUri":"no-pic","industry":"Software & Programming","country":"China","revenue":321.0,
    /// "marketValue":39253.0,"headquarters":"China","ceo":"Hong Zhou","profits":10.0,"assets":3637.0
    /// </summary>
    class MainCompanyInfo
    {
        public int Position { get; set; }
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
        public string ImageUri { get; set; }
        public string Industry { get; set; }
        public string Country { get; set; }
        public decimal Revenue { get; set; }
        public decimal MarketValue { get; set; }
        public string Headquarters { get; set; }
        public string Ceo { get; set; }
        public decimal Profits { get; set; }
        public decimal Assets { get; set; }
        public string State { get; set; }
        public string SquareImage { get; set; }
        public string Thumbnail { get; set; }
    }
}
