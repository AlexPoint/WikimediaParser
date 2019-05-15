using CompanyScrapper.Src.Forbes;
using IronWebScraper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace CompanyScrapper.Src.Parsers
{
    class ForbesGlobal2000Parser 
    {
        public List<MainCompanyInfo> ParseMainCompanyInfo(int year)
        {
            var url = string.Format("https://www.forbes.com/ajax/list/data?year={0}&uri=global2000&type=organization", year);

            WebRequest request = WebRequest.Create(url);
            var response = request.GetResponse();
            using (var dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                string responseFromServer = reader.ReadToEnd();
                // Display the content.  
                var infos = JsonConvert.DeserializeObject<List<MainCompanyInfo>>(responseFromServer);
                return infos;
            }
        }



    }
}
