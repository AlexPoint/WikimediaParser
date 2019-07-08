using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ETL.Src;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ETL
{
    class Program
    {
        static void Main(string[] args)
        {
            //ForbesCsvToRaw();
            Console.WriteLine("=======================");
            ForbesRawToCompanyDb();
        }

        public static void ForbesCsvToRaw()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json");

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            var timestamp = DateTime.Now.ToString("yyyyMMdd");

            // Create control flow
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(config["connectionString"]));

            // Create database
            var dbName = string.Format("tucdb_{0}", timestamp);
            CreateDatabaseTask.Create(dbName);

            // Create table for Forbes 2018 company data
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", config["connectionString"], dbName)));
            CreateTableTask.Create("dbo.Forbes2018Raw", new List<TableColumn>()
            {
                new TableColumn("ID", "int", allowNulls: false, isPrimaryKey:true, isIdentity:true),
                new TableColumn("Name", "nvarchar(max)", allowNulls: false),
                new TableColumn("Rank", "int", allowNulls: true),
                new TableColumn("Position", "int", allowNulls: true),
                new TableColumn("Uri", "nvarchar(max)", allowNulls: true),
                new TableColumn("ImageUri", "nvarchar(max)", allowNulls: true),
                new TableColumn("Industry", "nvarchar(50)", allowNulls: true),
                new TableColumn("Country", "nvarchar(50)", allowNulls: true),
                new TableColumn("Revenue", "decimal", allowNulls: true),
                new TableColumn("MarketValue", "decimal", allowNulls: true),
                new TableColumn("Headquarters", "nvarchar(max)", allowNulls: true),
                new TableColumn("Ceo", "nvarchar(50)", allowNulls: true),
                new TableColumn("Profits", "decimal", allowNulls: true),
                new TableColumn("Assets", "decimal", allowNulls: true),
                new TableColumn("State", "nvarchar(50)", allowNulls: true),
                new TableColumn("SquareImage", "nvarchar(max)", allowNulls: true),
                new TableColumn("Thumbnail", "nvarchar(max)", allowNulls: true)
            });

            // Load CSV file into SQL 
            CSVSource source = new CSVSource("C:/Users/Alex/Documents/Github/WikimediaParser/CompanyScrapper/Results/forbes-top-2000-companies-2018.csv");
            source.Configuration.Delimiter = ";";

            RowTransformation<string[], ForbesCompanyData> row = new RowTransformation<string[], ForbesCompanyData>
            (
                input => new ForbesCompanyData(){
                    Position = int.Parse(input[0]),
                    Rank = int.Parse(input[1]),
                    Name = input[2],
                    Uri = input[3],
                    ImageUri = input[4],
                    Industry = input[5],
                    Country = input[6],
                    Revenue = decimal.Parse(input[7]),
                    MarketValue = decimal.Parse(input[8]),
                    Headquarters = input[9],
                    Ceo = input[10],
                    Profits = decimal.Parse(input[11]),
                    Assets = decimal.Parse(input[12]),
                    State = input[13],
                    SquareImage = input[14],
                    Thumbnail = input[15]
                }
            );
            DBDestination<ForbesCompanyData> dest = new DBDestination<ForbesCompanyData>("dbo.Forbes2018Raw");

            source.LinkTo(row);
            row.LinkTo(dest);

            source.Execute();
            dest.Wait();

            int rowCount = RowCountTask.Count("dbo.Forbes2018Raw").Value;
            Console.WriteLine("Inserted {0} rows in dbo.Forbes2018Raw", rowCount);

            /*DBSource<MainCompanyInfo> source = new DBSource<MainCompanyInfo>("select * from dbo.Source");
            RowTransformation<MySimpleRow, MySimpleRow> trans = new RowTransformation<MySimpleRow, MySimpleRow>(
                myRow => {
                    myRow.Value += 1;
                    return myRow;
                });
            DBDestination<MainCompanyInfo> dest = new DBDestination<MainCompanyInfo>("dbo.Destination");*/

        }

        public static void ForbesRawToCompanyDb()
        {
            var targetTableName = "dbo.Forbes2018";

            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json");

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            var timestamp = DateTime.Now.ToString("yyyyMMdd");

            // Create control flow
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(config["connectionString"]));

            // Create database
            var dbName = string.Format("tucdb_{0}", timestamp);
            CreateDatabaseTask.Create(dbName);

            // Create table for Forbes 2018 company data
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", config["connectionString"], dbName)));

            CreateTableTask.Create("dbo.Forbes2018", new List<TableColumn>()
            {
                new TableColumn("ID", "int", allowNulls: false, isPrimaryKey:true, isIdentity:true),
                new TableColumn("Name", "nvarchar(max)", allowNulls: false),
                new TableColumn("Industry", "nvarchar(50)", allowNulls: true),
                new TableColumn("Country", "nvarchar(50)", allowNulls: true),
                new TableColumn("Revenue_2018_mUSD", "decimal", allowNulls: true),
                new TableColumn("MarketValue_2018_mUSD", "decimal", allowNulls: true),
                new TableColumn("Headquarters", "nvarchar(max)", allowNulls: true),
                new TableColumn("Ceo_2018", "nvarchar(50)", allowNulls: true),
                new TableColumn("Profits_2018_mUSD", "decimal", allowNulls: true),
                new TableColumn("Assets_2018_mUSD", "decimal", allowNulls: true)
            });
            
            var source = new DBSource<ForbesCompanyData>("select ID, Name, Rank, Position, Uri, ImageUri, Industry, Country, Revenue, MarketValue, Headquarters, Ceo, Profits, Assets, State, SquareImage, Thumbnail from dbo.Forbes2018Raw");

            /*var rows = new List<ForbesCompanyData>();
            var dest = new CustomDestination<ForbesCompanyData>(
                row => {
                    rows.Add(row);
                }
            );

            source.LinkTo(dest);
            source.Execute();
            dest.Wait();*/


            RowTransformation<ForbesCompanyData, CompanyData> trans = new RowTransformation<ForbesCompanyData, CompanyData>(
                myRow => new CompanyData {
                     Name = myRow.Name,
                     Assets_2018_mUSD = myRow.Assets,
                     Profits_2018_mUSD = myRow.Profits,
                     Revenue_2018_mUSD = myRow.Revenue,
                     Industry = myRow.Industry,
                     Country = myRow.Country,
                     MarketValue_2018_mUSD = myRow.MarketValue,
                     Ceo_2018 = myRow.Ceo,
                     Headquarters = myRow.Headquarters != myRow.Country ? string.Format("{0}, {1}", myRow.Headquarters, myRow.Country): myRow.Country
                });
            DBDestination<CompanyData> dest = new DBDestination<CompanyData>("dbo.Forbes2018");

            source.LinkTo(trans);
            trans.LinkTo(dest);

            source.Execute();
            dest.Wait();

            int rowCount = RowCountTask.Count(targetTableName).Value;
            Console.WriteLine("Inserted {0} rows in table '{1}'", rowCount, targetTableName);
        }

    }
}
