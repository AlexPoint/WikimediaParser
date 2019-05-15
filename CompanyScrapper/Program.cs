using System;
using System.IO;
using System.Linq;
using System.Text;
using CompanyScrapper.Src.Forbes;
using CompanyScrapper.Src.Parsers;
using CsvHelper;
using IronWebScraper;

namespace CompanyScrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            var applicationDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/../../../";

            var year = 2018;
            var fileName = string.Format("forbes-top-2000-companies-{0}.csv", year);
            var filePath = applicationDirectory + "Results/" + fileName;

            var forbesScrapper = new ForbesGlobal2000Parser();
            var mainInfos = forbesScrapper.ParseMainCompanyInfo(year);

            Console.WriteLine("Parsed {0} companies", mainInfos.Count);

            using (var writer = new StreamWriter(filePath))
            using (var csvWriter = new CsvWriter(writer))
            {
                csvWriter.Configuration.Delimiter = ";";
                csvWriter.Configuration.HasHeaderRecord = true;
                csvWriter.Configuration.AutoMap<MainCompanyInfo>();

                csvWriter.WriteHeader<MainCompanyInfo>();
                csvWriter.NextRecord();
                csvWriter.WriteRecords(mainInfos);

                writer.Flush();
            }

            Console.WriteLine("Wrote those information in {0}", filePath);
        }
    }
}
