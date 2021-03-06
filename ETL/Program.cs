﻿using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ETL.Src;
using ETL.Src.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ETL
{
    class Program
    {
        private static NLog.Logger EtlFlowLogger = NLog.LogManager.GetLogger("EtlFlow");

        static void Main(string[] args)
        {
            //ForbesDataEtl();

            WikiDumpsEtl();
        }


        // Wiki dumps ETL -------------------------------------------------------------------------------------------------------

        public static void WikiDumpsEtl()
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json");
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            var connectionString = config["connectionString"];
            var dbName = "tucdb";

            // ----------------------------------------------------------------------------------------
            // 1. Copying another database with the raw infobox properties, i.e:
            // - the wikipedia page title
            // - the infobox property key (e.g. revenues)
            // - the infobox property values (e.g. 165 billion [[USD]]
            // - the infobox id (a few pages have multiple infoboxes)

            // Load directly infobox properties from db wikiboxes, table RawInfoboxProperties as ETLBox allows transfer between databases.
            // (we were using csv files as an intermediate step before but it triggered issues due to badly form CSV rows).
            CopyTable(connectionString, "wikiboxes", "dbo.RawInfoboxProperties", connectionString, dbName, "dbo.WikiInfoboxPropertiesRaw");

            // Infobox property keys do not always meet the Wiki standards (see https://en.wikipedia.org/wiki/Template:Infobox_company)
            // As a result, we "clean" the names here.
            Func<string, string> t1 = s => string.IsNullOrEmpty(s) ? null : s.ToLower();
            TransformColumn(connectionString, dbName, "WikiInfoboxPropertiesRaw", "PropKey", t1);
            Func<string, string> t2 = s => string.IsNullOrEmpty(s) ? null : Regex.Replace(s, @"(^[^a-z]+|[^a-z]+$)", "");
            TransformColumn(connectionString, dbName, "WikiInfoboxPropertiesRaw", "PropKey", t2);
            Func<string, string> t3 = s => string.IsNullOrEmpty(s) ? null : Regex.Replace(s.ToLower(), @"[^a-z]+", "_");
            TransformColumn(connectionString, dbName, "WikiInfoboxPropertiesRaw", "PropKey", t3);
            Func<string, string> t4 = s => string.IsNullOrEmpty(s) ? null : s.Substring(0, Math.Min(s.Length, 128));
            TransformColumn(connectionString, dbName, "WikiInfoboxPropertiesRaw", "PropKey", t4);

            // Specific delete query
            DeleteInfrequentInfoboxProperties(connectionString, dbName, "WikiInfoboxPropertiesRaw", "PropKey", 100);

            // ----------------------------------------------------------------------------------------
            // 2. Pivot properties to have one row per infobox company

            PivotProperties(connectionString, dbName, "dbo.WikiInfoboxPropertiesRaw", "dbo.TestWikiCompanyDataRaw");

            // ----------------------------------------------------------------------------------------
            // 3. Clean the infobox properties that we want to keep.

            // ISIN number
            var isinRegex = new Regex("^[A-Z\\d]{12}$", RegexOptions.Compiled);
            Func<string, string> cleanIsin = s => string.IsNullOrEmpty(s) || !isinRegex.IsMatch(s) ? null : s;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "isin", cleanIsin);

            // Revenue year
            var revYearRegex = new Regex(@"\((?:FY\s+)?([\d\s]+)\)", RegexOptions.Compiled);
            Func<string, string> extractRevenueYear = s => string.IsNullOrEmpty(s) || !revYearRegex.IsMatch(s) ?
                 null : revYearRegex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "revenue", "revenue_year2", extractRevenueYear);

            var cleanRevYearRegex = new Regex(@"(\d{4}(?:\-\d{2})?)", RegexOptions.Compiled);
            Func<string, string> cleanRevenueYear = s => string.IsNullOrEmpty(s) || !cleanRevYearRegex.IsMatch(s) ?
                 null : cleanRevYearRegex.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "revenue_year", cleanRevenueYear);

            Func<string, string, string> mergeRevYear = (s1, s2) => !string.IsNullOrEmpty(s1) ? s1 : s2;
            MergeColumns(connectionString, dbName, "TestWikiCompanyDataRaw", "revenue_year", "revenue_year2", "revenue_year3", mergeRevYear);

            DeleteColumns(connectionString, dbName, "TestWikiCompanyDataRaw", new List<string>() { "caption", "revenue_year2" });

            RenameColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "revenue_year3", "revenue_year");

            // Revenue
            ExtractInformationFromMoneyColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "revenue", "revenue_amount", "revenue_currency", "revenue_src_url");
            
            // 5. Nb employees (year)
            var nbEmployeeYearRegex = new Regex(@"\(\b(\d{4})\b\)", RegexOptions.Compiled);
            Func<string, string> extractNbEmployeeYear = s => string.IsNullOrEmpty(s) || !nbEmployeeYearRegex.IsMatch(s) ?
                 null : nbEmployeeYearRegex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "num_employees", "num_employees_year2", extractNbEmployeeYear);

            var cleanNbEmployeeYearRegex = new Regex(@"(\b\d{4}\b)", RegexOptions.Compiled);
            Func<string, string> cleanNbEmployeeYear = s => string.IsNullOrEmpty(s) || !cleanNbEmployeeYearRegex.IsMatch(s) ?
                 null : cleanNbEmployeeYearRegex.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "num_employees_year", cleanNbEmployeeYear);

            Func<string, string, string> mergeNbEmployeesYear = (s1, s2) => !string.IsNullOrEmpty(s1) ? s1 : s2;
            MergeColumns(connectionString, dbName, "TestWikiCompanyDataRaw", "num_employees_year", "num_employees_year2", "num_employees_year3", mergeNbEmployeesYear);

            RenameColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "num_employees_year3", "num_employees_year");
            
            // 6. Nb employees
            var extractNbEmployeesRegex = new Regex(@"(\b\d[\d\.\s,]*\d\b)", RegexOptions.Compiled);
            Func<string, string> extractNbEmployees = s => string.IsNullOrEmpty(s) || !extractNbEmployeesRegex.IsMatch(s) ?
                 null : extractNbEmployeesRegex.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "num_employees", extractNbEmployees);

            var cleanNbEmployeesRegex = new Regex(@"[^\d]+", RegexOptions.Compiled);
            Func<string, string> cleanNbEmployees = s => string.IsNullOrEmpty(s) ? null : cleanNbEmployeesRegex.Replace(s, "");
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "num_employees", cleanNbEmployees);

            // 7. Net income (year)
            var netIncomeYearRegex = new Regex(@"\(\b(\d{4})\b\)", RegexOptions.Compiled);
            Func<string, string> extractNetIncomeYear = s => string.IsNullOrEmpty(s) || !netIncomeYearRegex.IsMatch(s) ?
                 null : netIncomeYearRegex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "net_income", "net_income_year2", extractNetIncomeYear);

            var cleanNetIncomeYearRegex = new Regex(@"(\b\d{4}\b)", RegexOptions.Compiled);
            Func<string, string> cleanNetIncomeYear = s => string.IsNullOrEmpty(s) || !cleanNetIncomeYearRegex.IsMatch(s) ?
                 null : cleanNetIncomeYearRegex.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "net_income_year", cleanNetIncomeYear);

            Func<string, string, string> mergeNetIncomeYear = (s1, s2) => !string.IsNullOrEmpty(s1) ? s1 : s2;
            MergeColumns(connectionString, dbName, "TestWikiCompanyDataRaw", "net_income_year", "net_income_year2", "net_income_year3", mergeNetIncomeYear);

            RenameColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "net_income_year3", "net_income_year");

            // 8. Net income (amount)
            ExtractInformationFromMoneyColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "net_income", "net_income_amount", "net_income_currency", "net_income_src_url");
            
            // 9. Operating income (year)
            var opIncomeYearRegex = new Regex(@"\(\b(\d{4})\b\)", RegexOptions.Compiled);
            Func<string, string> extractOpIncomeYear = s => string.IsNullOrEmpty(s) || !opIncomeYearRegex.IsMatch(s) ?
                 null : opIncomeYearRegex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "operating_income", "operating_income_year", extractOpIncomeYear);

            // 10. Operating income (amount)
            ExtractInformationFromMoneyColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "operating_income", "operating_income_amount", 
                "operating_income_currency", "operating_income_src_url");
            
            // 11. Industry
            var cleanIndustryRegex = new Regex(@"\[\[([^\|\]]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled);
            Func<string, string> cleanIndustry = s => string.IsNullOrEmpty(s) ?
                 null : cleanIndustryRegex.Replace(s, "$1");
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "industry", cleanIndustry);

            // 
            var cleanIndustryRegex2 = new Regex(@"[\s\.]*<br[\s\/]*>\s*", RegexOptions.Compiled);
            Func<string, string> cleanIndustry2 = s => string.IsNullOrEmpty(s) ? null : cleanIndustryRegex2.Replace(s, ", ");
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "industry", cleanIndustry2);

            var cleanIndustryRegex3 = new Regex(@"([\s\n]+)?\*([\s\n]+)?", RegexOptions.Compiled);
            Func<string, string> cleanIndustry3 = s => string.IsNullOrEmpty(s) ? null : cleanIndustryRegex3.Replace(s, ", ");
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "industry", cleanIndustry3);

            var cleanIndustryRegex4 = new Regex(@"\{\{[^\}]+list\s*\|[\n\s]*([^\n\}]*)", RegexOptions.Compiled);
            Func<string, string> cleanIndustry4 = s => string.IsNullOrEmpty(s) || !cleanIndustryRegex4.IsMatch(s) ?
                s : cleanIndustryRegex4.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "industry", cleanIndustry4);

            Func<string, string> cleanIndustry5 = s => string.IsNullOrEmpty(s) ? null : s.Trim(new char[] { ' ', ',' });
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "industry", cleanIndustry5);

            var cleanIndustryRegex6 = new Regex(@"^([\w\s\-&,\(\)]+)", RegexOptions.Compiled);
            Func<string, string> cleanIndustry6 = s => string.IsNullOrEmpty(s) || !cleanIndustryRegex6.IsMatch(s) ?
                null : cleanIndustryRegex6.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "industry", cleanIndustry6);

            // 12. Country
            // TODO: catch cases such as {{nowrap|United States}}, (Germany), {{unbulleted list|Israel}}...
            var cleanLocationCountryRegex = new Regex(@"\[\[([^\|\]]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled);
            Func<string, string> cleanLocationCountry = s => string.IsNullOrEmpty(s) || !cleanLocationCountryRegex.IsMatch(s) ?
                 null : cleanLocationCountryRegex.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "location_country", cleanLocationCountry);

            var cleanHqLocationCountryRegex = new Regex(@"\[\[([^\|\]]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled);
            Func<string, string> cleanHqLocationCountry = s => string.IsNullOrEmpty(s) || !cleanHqLocationCountryRegex.IsMatch(s) ?
                 null : cleanHqLocationCountryRegex.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "hq_location_country", cleanHqLocationCountry);

            Func<string, string, string> mergeLocCountry = (s1, s2) => !string.IsNullOrEmpty(s1) ? s1 : s2;
            MergeColumns(connectionString, dbName, "TestWikiCompanyDataRaw", "location_country", "hq_location_country", "hq_country", mergeLocCountry);
            
            // 13. Name & PageTitle
            var cleanNameRegex1 = new Regex("^([^<]+)", RegexOptions.Compiled);
            Func<string, string> cleanName1 = s => string.IsNullOrEmpty(s) || !cleanNameRegex1.IsMatch(s) ?
                null : cleanNameRegex1.Match(s).Groups[1].Value;
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "name", cleanName1);

            Func<string, string> cleanName2 = s => string.IsNullOrEmpty(s) ?
                null : s.Replace("\"", "'");
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "name", cleanName2);
            TransformColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "PageTitle", cleanName2);

            // TODO: clean other valuable columns:
            // - assets
            // - market_cap (never filled correctly on a 25k sample)
            // - CEO
            // - type (public, private...)
            // - chairman
            // - founded 
            // - headquarters

            RenameColumn(connectionString, dbName, "TestWikiCompanyDataRaw", "PageTitle", "wiki_name");

            // ----------------------------------------------------------------------------------------
            // 4. Remove all useless columns
            var columnsToKeep = new List<string>() { "wiki_name",
                "name",
                "isin",
                "revenue_year",
                "revenue_amount",
                "revenue_src_url",
                "revenue_currency",
                "num_employees_year",
                "num_employees",
                "net_income_year",
                "net_income_amount",
                "net_income_src_url",
                "net_income_currency",
                "operating_income_year",
                "operating_income_amount",
                "operating_income_src_url",
                "operating_income_currency",
                "industry",
                "hq_country"};
            KeepOnlyColumns(connectionString, dbName, "TestWikiCompanyDataRaw", columnsToKeep);
        }


        /// <summary>
        /// Extract the currency and the amount of a wikipedia infobox property, taking into account 
        /// the multiple format of a money column.
        /// For instance, the 'revenue' column '$19.692 [[1000000000 (number)|billion]] {{cite web|title=Sytner reports|url=http://www.am-online.com/news/dealer-news/2016/10/24/sytner-reports-record-profits-warns-of-drop-in-post-brexit-consumer-confidence|website=Automotive Management|publisher=AM-Online|accessdate=18 August 2017}})' 
        /// will be extracted to:
        /// - 'revenue_amount': '19.692 billion'
        /// - 'revenue_currency': '$'
        /// - 'revenue_src_url': 'http://www.am-online.com/news/dealer-news/2016/10/24/sytner-reports-record-profits-warns-of-drop-in-post-brexit-consumer-confidence'
        /// </summary>
        private static void ExtractInformationFromMoneyColumn(string connectionString, string dbName, string table, 
            string srcColumn, string tgtColumnAmount, string tgtColumnCurrency, string tgtColumnSrcUrl)
        {
            // 1. Clean the units with Wiki links (e.g. $19.692 [[1000000000 (number)|billion]] -> $19.692 billion)
            var cleanRevUnitsRegex = new Regex(@"\[\[(?:[^\|]+\|)?(million|billion)\]\]", RegexOptions.Compiled);
            Func<string, string> cleanRevUnits = s => string.IsNullOrEmpty(s) || !cleanRevUnitsRegex.IsMatch(s) ?
                  s : cleanRevUnitsRegex.Replace(s, "$1");
            TransformColumn(connectionString, dbName, table, srcColumn, cleanRevUnits);

            Func<string, string> cleanRevNbsp = s => string.IsNullOrEmpty(s) ? null : s.Replace("&nbsp;", " ");
            TransformColumn(connectionString, dbName, table, srcColumn, cleanRevNbsp);

            // 2. Extract the revenue amount
            var revAmountRegex = new Regex(@"((?:\-)?[\d\.\s,]+[\s]+(?:million|billion))", RegexOptions.Compiled);
            Func<string, string> extractRevenueAmount = s => string.IsNullOrEmpty(s) || !revAmountRegex.IsMatch(s) ?
                 null : revAmountRegex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, table, srcColumn, tgtColumnAmount, extractRevenueAmount);

            // 3. Extract the source url (for the revenue information)
            // Some revenue field contain a reference with the URL of the page where the information was found.
            // (e.g. {{cite web|title=Sytner reports record profits, warns of drop in post-Brexit consumer confidence|url=http://www.am-online.com/news/dealer-news/2016/10/24/sytner-reports-record-profits-warns-of-drop-in-post-brexit-consumer-confidence|website=Automotive Management|publisher=AM-Online|accessdate=18 August 2017}})
            var revSrcUrlRegex = new Regex(@"\{\{cite web\s*\|.*url=([^\|]+)\|", RegexOptions.Compiled);
            Func<string, string> extractRevenueSrcUrl = s => string.IsNullOrEmpty(s) || !revSrcUrlRegex.IsMatch(s) ?
                 null : revSrcUrlRegex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, table, srcColumn, tgtColumnSrcUrl, extractRevenueSrcUrl);

            // 4. Extract the currency
            // Multiple passes to cover most cases
            // 
            var cleanRevCurRegex = new Regex(@"\{\{0(\|0+)?\}\}", RegexOptions.Compiled);
            Func<string, string> cleanRevCur = s => string.IsNullOrEmpty(s) || !cleanRevCurRegex.IsMatch(s) ?
                  s : cleanRevCurRegex.Replace(s, "");
            TransformColumn(connectionString, dbName, table, srcColumn, cleanRevCur);

            var tgtColCur1 = tgtColumnAmount + "_1";
            var revCurrency1Regex = new Regex(@"\[\[([^\|\]]+)(?:\|[^\]]+)?\]\](?:[\$£€])?((?:\-)?[\d\.\s,]+[\s]+(?:million|billion))", RegexOptions.Compiled);
            Func<string, string> extractRevenueCurrency1 = s => string.IsNullOrEmpty(s) || !revCurrency1Regex.IsMatch(s) ?
                 null : revCurrency1Regex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, table, srcColumn, tgtColCur1, extractRevenueCurrency1);

            var tgtColCur2 = tgtColumnAmount + "_2";
            var revCurrency2Regex = new Regex(@"(?:[\$£€])?((?:\-)?[\d\.\s,]+[\s]+(?:million|billion))\s+\[\[([^\|]+)(?:\|[^\[]+)?\]\]", RegexOptions.Compiled);
            Func<string, string> extractRevenueCurrency2 = s => string.IsNullOrEmpty(s) || !revCurrency2Regex.IsMatch(s) ?
                 null : revCurrency2Regex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, table, srcColumn, tgtColCur2, extractRevenueCurrency2);

            var tgtColCur3 = tgtColumnAmount + "_3";
            var revCurrency3Regex = new Regex(@"\{\{([A-Z]{3})\|(?:[^\|]+\|)?((?:\-)?[\d\.\s,]+[\s]+(?:million|billion))\s*\}\}", RegexOptions.Compiled);
            Func<string, string> extractRevenueCurrency3 = s => string.IsNullOrEmpty(s) || !revCurrency3Regex.IsMatch(s) ?
                 null : revCurrency3Regex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, table, srcColumn, tgtColCur3, extractRevenueCurrency3);

            var tgtColCur4 = tgtColumnAmount + "_4";
            var revCurrency4Regex = new Regex(@"(?:[\$£€])?(?:\-)?[\d\.\s,]+[\s]+(?:million|billion)\s+\[\[([^\|\]]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled);
            Func<string, string> extractRevenueCurrency4 = s => string.IsNullOrEmpty(s) || !revCurrency4Regex.IsMatch(s) ?
             null : revCurrency4Regex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, table, srcColumn, tgtColCur4, extractRevenueCurrency4);

            var tgtColCur5 = tgtColumnAmount + "_5";
            var revCurrency5Regex = new Regex(@"([A-Z]*[\$£€¥])(?:\-)?[\d\.\s,]+[\s]+(?:million|billion)", RegexOptions.Compiled);
            Func<string, string> extractRevenueCurrency5 = s => string.IsNullOrEmpty(s) || !revCurrency5Regex.IsMatch(s) ?
             null : revCurrency5Regex.Match(s).Groups[1].Value;
            ExtractFromColumn(connectionString, dbName, table, srcColumn, tgtColCur5, extractRevenueCurrency5);

            Func<string, string, string> mergeCurrencyColumns = (s1, s2) => !string.IsNullOrEmpty(s1) ? s1 : s2;
            MergeColumns(connectionString, dbName, table, tgtColCur2, tgtColCur3, "col_currency_23", mergeCurrencyColumns);
            MergeColumns(connectionString, dbName, table, tgtColCur1, tgtColCur4, "col_currency_14", mergeCurrencyColumns);
            MergeColumns(connectionString, dbName, table, tgtColCur5, "col_currency_23", "col_currency_523", mergeCurrencyColumns);
            MergeColumns(connectionString, dbName, table, "col_currency_14", "col_currency_523", tgtColumnCurrency, mergeCurrencyColumns);
        }
        
        /// <summary>
        /// Delete the infobox properties which occur few times in the whole Wikipedia corpus.
        /// This also helps ignoring spelling mistakes.
        /// </summary>
        private static void DeleteInfrequentInfoboxProperties(string connectionString, string db, string table, string column, int threshold)
        {
            // Create control flow
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(connectionString));

            // Create table for Forbes 2018 company data
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            var deleteQuery = new SqlTask("Delete infrequent properties",
                string.Format("DELETE from [{0}] where {1} in (select {1} from [{0}] group by {1} having count(*) <= {2})",
                table, column, threshold));
            deleteQuery.Execute();
        }

        /// <summary>
        /// Delete all the columns of a given table except the ones listed in the 'columns' parameter.
        /// </summary>
        private static void KeepOnlyColumns(string connectionString, string db, string table, List<string> columns)
        {
            // Create control flow 
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            var existingColumns = GetTableColumns(connectionString, db, table);
            var columnsToDelete = existingColumns.Where(col => !columns.Contains(col.Name)).Select(col => col.Name).ToList();

            DeleteColumns(connectionString, db, table, columnsToDelete);
        }

        /// <summary>
        /// Delete specific columns in a given SQL table.
        /// </summary>
        private static void DeleteColumns(string connectionString, string db, string table, List<string> columns)
        {
            // Create control flow 
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            var existingColumns = GetTableColumns(connectionString, db, table);
            var missingColumns = columns.Where(col => !existingColumns.Any(eCol => eCol.Name == col)).ToList();

            if (missingColumns.Any())
            {
                EtlFlowLogger.Warn("Tried to delete {0} columns missing in table {1}: {2}. Skipping the deletion of those columns.", 
                    missingColumns.Count, table, string.Join(", ", missingColumns));
            }

            var colsToDelete = columns.Except(missingColumns).ToList();
            if (colsToDelete.Any())
            {
                var colQuery = string.Join(", ", colsToDelete.Select(col => string.Format("[{0}]", col)));
                var dropTempCols = new SqlTask("Drop temp column", string.Format("ALTER TABLE [{0}] DROP COLUMN {1}", table, colQuery));
                dropTempCols.Execute(); 
            }
        }

        /// <summary>
        /// Lists all the columns of a given SQL table.
        /// </summary>
        private static List<TableColumn> GetTableColumns(string connectionString, string db, string table)
        {
            // Read data from SQL server
            var dataTable = new DataTable();
            var conn = new SqlConnection(string.Format("{0};Database={1}", connectionString, db));
            try
            {
                conn.Open();
                var selectQuery = string.Format("SELECT * FROM {0}", table);
                var command = new SqlCommand(selectQuery, conn);
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
            
            // Put the results in memory (column names, and then data)
            int icolcount = dataTable.Columns.Count;

            var cols = new List<TableColumn>();
            for (int i = 0; i < icolcount; i++)
            {
                var col = dataTable.Columns[i];

                var dbType = DataTypeConverter.GetDbTypeString(col.DataType.ToString());
                var tableCol = new TableColumn()
                {
                    Name = col.ColumnName,
                    DataType = dbType.ToString(),
                    AllowNulls = col.AllowDBNull
                    // IsPrimaryKey & IsIdentity cannot be filled from the DataColumn object only
                };
                cols.Add(tableCol);
            }

            return cols;
        }

        /// <summary>
        /// Merge two columns of a given SQL table based on a merge function.
        /// </summary>
        /// <param name="connectionString">The connection string to the database</param>
        /// <param name="db">The name of the database</param>
        /// <param name="table">The name of the SQL table</param>
        /// <param name="srcColumn1">The first column to merge</param>
        /// <param name="srcColumn2">The second column to merge</param>
        /// <param name="tgtColumn">The target column which will receive the result of the merge</param>
        /// <param name="merge">The merge function, i.e. how the two columns should be merged</param>
        /// <returns></returns>
        public static string MergeColumns(string connectionString, string db, string table, string srcColumn1, string srcColumn2, string tgtColumn, 
            Func<string, string, string> merge)
        {
            EtlFlowLogger.Info("------------");
            EtlFlowLogger.Info("Executing MergeColumns task (table = {0}, srcColumn1 = {1}, srcColumn2 = {2})", table, srcColumn1, srcColumn2);

            // Create control flow 
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            // Check the existence of the table and the source column, and the non-existence of the target column
            var tableExist = DoesTableExist(connectionString, db, table);
            if (!tableExist)
            {
                EtlFlowLogger.Warn("Cannot MergeColumns from a non-existing table: {0}. Skipping operation.", table);
                return "";
            }
            if (!DoesColumnExist(connectionString, db, table, srcColumn1))
            {
                EtlFlowLogger.Warn("Cannot MergeColumns with non-existing source column {0}. Skipping operation.", srcColumn1);
                return "";
            }
            if (!DoesColumnExist(connectionString, db, table, srcColumn2))
            {
                EtlFlowLogger.Warn("Cannot MergeColumns with non-existing source column {0}. Skipping operation.", srcColumn2);
                return "";
            }
            // TODO: allow case where target column is one of the source columns?
            if (DoesColumnExist(connectionString, db, table, tgtColumn))
            {
                EtlFlowLogger.Warn("Unexpected behaviour when merging columns '{0}' & '{1}' to an already existing column '{2}' in table '{3}'. Skipping operation.",
                    srcColumn1, srcColumn2, tgtColumn, table);
                return "";
            }


            // Rename current table to temporary name and create a copy of this table with an additional column.
            // With ETLBox, we cannot update columns inside the same table. We have to create a new table and a flow between the two tables.
            var tempTable = string.Format("{0}Temp", table);

            // TODO: check if the temp table already exists?
            DropTableTask.Drop(tempTable);
            var createTempTableTask = new SqlTask("Rename to temp table", string.Format(@"EXEC sp_rename '{0}', '{1}';", table, tempTable));
            createTempTableTask.Execute();

            // Add a copy of the table
            var columns = GetTableColumns(connectionString, db, tempTable);
            /*var newColumns = columns.Where(col => col.Name != srcColumn1 & col.Name != srcColumn2).ToList();*/

            var srcCol1 = columns.First(c => c.Name == srcColumn1);
            columns.Add(new TableColumn(tgtColumn, srcCol1.DataType, srcCol1.AllowNulls));

            // We need to create a new table as destination (source and destination cannot be the same).
            CreateTableTask.Create(table, columns);

            var oldColumnIndex1 = columns.FindIndex(col => col.Name == srcColumn1);
            var oldColumnIndex2 = columns.FindIndex(col => col.Name == srcColumn2);
            var newColumnIndex = columns.FindIndex(col => col.Name == tgtColumn);

            var source = new DBSource(tempTable);
            Func<string[], string[]> rowTransFunc = arr =>
            {
                Array.Resize(ref arr, arr.Length + 1);
                arr[newColumnIndex] = merge(arr[oldColumnIndex1], arr[oldColumnIndex2]);
                return arr;
            };
            var trans = new RowTransformation(rowTransFunc);
            var dest = new DBDestination(table);

            source.LinkTo(trans);
            trans.LinkTo(dest);

            source.Execute();
            dest.Wait();


            // Log information about the result of the trasnformation
            var countTask = new RowCountTask(table, string.Format("{0} is not NULL", tgtColumn));
            EtlFlowLogger.Info("{0} rows have been updated", countTask.Count().Rows);

            string[] curCol = null;
            var examples = new List<string[]>();
            var sql = string.Format("select distinct {0}, {1}, {2} from {3} where {2} is not NULL", srcColumn1, srcColumn2, tgtColumn, table);
            var findExamplesTask = new SqlTask("Select a few examples",
                sql,
                () => {
                    curCol = new string[3];
                },
                () => {
                    examples.Add(curCol);
                },
                col => curCol[1] = col != null ? col.ToString(): null,
                tempCol => curCol[0] = tempCol != null ? tempCol.ToString(): null,
                tgtCol => curCol[2] = tgtCol != null ? tgtCol.ToString(): null)
            {

                ReadTopX = 5
            };
            findExamplesTask.ExecuteReader();
            foreach (var example in examples)
            {
                EtlFlowLogger.Info("'{0}' + '{1}' => {2}", example[0], example[1], example[2]);
            }

            // Cleanup behind by dropping the temp table and source columns. 
            DropTableTask.Drop(tempTable);
            var dropTempCols = new SqlTask("Drop temp column", string.Format("ALTER TABLE [{0}] DROP COLUMN [{1}], [{2}]", table, srcColumn1, srcColumn2));
            dropTempCols.Execute();
            EtlFlowLogger.Info("End of execution of MergeColumns task");

            return table;
        }

        /// <summary>
        /// Extracts from a given source column to a target column.
        /// The source column is left untouched; the target column is freshly created.
        /// </summary>
        public static string ExtractFromColumn(string connectionString, string db, string table, string srcColumn, string tgtColumn, Func<string,string> extract)
        {
            EtlFlowLogger.Info("------------");
            EtlFlowLogger.Info("Executing ExtractFromColumn task (table = {0}, srcColumn = {1}, tgtColumn = {2})", table, srcColumn, tgtColumn);

            // Create control flow 
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            // Check the existence of the table and the source column, and the non-existence of the target column
            var tableExist = DoesTableExist(connectionString, db, table);
            if (!tableExist)
            {
                EtlFlowLogger.Warn("Cannot ExtractColumn from a non-existing table: {0}. Skipping operation.", table);
                return "";
            }
            if(!DoesColumnExist(connectionString, db, table, srcColumn))
            {
                EtlFlowLogger.Warn("Cannot ExtractColumn from a non-existing column '{0}' in table '{1}'. Skipping operation.", srcColumn, table);
                return "";
            }
            if(DoesColumnExist(connectionString, db, table, tgtColumn))
            {
                EtlFlowLogger.Warn("Unpexected behaviour when extracting column '{0}' to an already existing column '{1}' in table '{2}'. Skipping operation.",
                    srcColumn, tgtColumn, table);
                return "";
            }

            // Rename current table to temporary name and create a copy of this table with an additional column.
            // With ETLBox, we cannot update columns inside the same table. We have to create a new table and a flow between the two tables.
            var tempTable = string.Format("{0}Temp", table);

            // TODO: check if the temp table already exists?
            DropTableTask.Drop(tempTable);
            var createTempTableTask = new SqlTask("Rename to temp table", string.Format(@"EXEC sp_rename '{0}', '{1}';", table, tempTable));
            createTempTableTask.Execute();

            // Add a copy of the table
            var columns = GetTableColumns(connectionString, db, tempTable);
            var srcCol = columns.First(c => c.Name == srcColumn);
            columns.Add(new TableColumn(tgtColumn, srcCol.DataType, srcCol.AllowNulls));

            // We need to create a new table as destination (source and destination cannot be the same).
            CreateTableTask.Create(table, columns);

            var oldColumnIndex = columns.FindIndex(col => col.Name == srcColumn);
            var newColumnIndex = columns.FindIndex(col => col.Name == tgtColumn);

            var source = new DBSource(tempTable);
            Func<string[], string[]> rowTransFunc = arr =>
            {
                Array.Resize(ref arr, arr.Length + 1);
                arr[newColumnIndex] = extract(arr[oldColumnIndex]);
                return arr;
            };
            var trans = new RowTransformation(rowTransFunc);
            var dest = new DBDestination(table);

            source.LinkTo(trans);
            trans.LinkTo(dest);

            source.Execute();
            dest.Wait();


            // Log information about the result of the trasnformation
            var countTask = new RowCountTask(table, string.Format("{0} is not NULL", tgtColumn));
            EtlFlowLogger.Info("{0} rows have been updated", countTask.Count().Rows);

            string[] curCol = null;
            var examples = new List<string[]>();
            var sql = string.Format("select distinct {0}, {1} from {2} where {1} is not NULL", srcColumn, tgtColumn, table);
            var findExamplesTask = new SqlTask("Select a few examples",
                sql,
                () => {
                    curCol = new string[2];
                },
                () => {
                    examples.Add(curCol);
                },
                col => curCol[1] = col.ToString(),
                tempCol => curCol[0] = tempCol.ToString())
            {

                ReadTopX = 5
            };
            findExamplesTask.ExecuteReader();
            foreach (var example in examples)
            {
                EtlFlowLogger.Info("{0} => {1}", example[1], example[0]);
            }

            // Cleanup behind by dropping the temp table 
            DropTableTask.Drop(tempTable);
            EtlFlowLogger.Info("End of execution of ExtractFromColumn task");

            return table;
        }

        /// <summary>
        /// Checks if a column exists in a given table.
        /// </summary>
        private static bool DoesColumnExist(string connectionString, string db, string table, string column)
        {
            var columns = GetTableColumns(connectionString, db, table);
            return columns.Any(c => c.Name == column);
        }

        /// <summary>
        /// Checks if a table exists in a given database
        /// </summary>
        private static bool DoesTableExist(string connectionString, string db, string table)
        {
            var tables = GetTables(connectionString, db);
            return tables.Any(t => t == table);
        }

        /// <summary>
        /// Lists all the tables in a given database.
        /// </summary>
        private static List<string> GetTables(string connectionString, string db)
        {
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            var tables = new List<string>();
            var getTables = new SqlTask("", "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES",
                s => tables.Add(s.ToString()));
            getTables.ExecuteReader();

            return tables;
        }

        /// <summary>
        /// Renames a given column of a SQL table
        /// </summary>
        private static void RenameColumn(string connectionString, string db, string table, string column, string newColumn)
        {
            EtlFlowLogger.Info("------------");
            EtlFlowLogger.Info("Executing RenameColumn task (table = {0}, column = {1})", table, column);

            // Create control flow and db (should already exist)
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(connectionString);
            CreateDatabaseTask.Create(db);
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            // TODO: check the existence of the table and the column, otherwise return
            // TODO: check the non-existence of the target column

            var alterTableTask = new SqlTask("Rename column", string.Format(@"EXEC sp_RENAME '[{0}].[{1}]', '{2}', 'COLUMN';", table, column, newColumn));
            alterTableTask.Execute();

            EtlFlowLogger.Info("End of execution of TransformColumn task");
        }

        /// <summary>
        /// Transforms the values in a given column, following a transform function.
        /// </summary>
        /// <param name="connectionString">The connection string for the database</param>
        /// <param name="db">The name of the database</param>
        /// <param name="table">The name of the table</param>
        /// <param name="column">The column to be trasnformed</param>
        /// <param name="transform">The transform function</param>
        /// <returns></returns>
        private static string TransformColumn(string connectionString, string db, string table, string column, Func<string, string> transform)
        {
            EtlFlowLogger.Info("------------");
            EtlFlowLogger.Info("Executing TransformColumn task (table = {0}, column = {1})", table, column);

            // Create control flow and db (should already exist)
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(connectionString);
            CreateDatabaseTask.Create(db);
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, db)));

            // TODO: check the existence of the table and the column, otherwise return

            // ETLBox does not allow for altering a table (such as adding a new column) and for "migrating" data into the same table (such as updating a column).
            // As a result, we use a convoluted way to update a column of a given table:
            // - we rename the table (temp table)
            // - we add a new column
            // - we create a new table with the name of the initial (now renamed) table and with the same schema
            // - we use ETLBox to transfer data from the temp table to the new table, with the given transformation
            // - we drop the temp table
            var tempTable = string.Format("{0}Temp", table);
            var tempColumn = string.Format("{0}Temp", column);

            DropTableTask.Drop(tempTable);
            var createTempTableTask = new SqlTask("Configure temp table", string.Format(@"EXEC sp_rename '{0}', '{1}';
                                                    EXEC sp_RENAME '[{1}].[{2}]', '{3}', 'COLUMN';
                                                    ALTER TABLE [{1}] ADD {2} NVARCHAR(MAX) NULL;", table, tempTable, column, tempColumn));
            createTempTableTask.Execute();

            // Create new table, with the original table name, which replicates the temp table (same columns), based on the source table schema (auto configuration).
            var columns = GetTableColumns(connectionString, db, tempTable);

            // We need to create a new table as destination (source and destination cannot be the same).
            // Drop the table just in case. 
            // TODO: make sure that table names or unique
            DropTableTask.Drop(table);
            CreateTableTask.Create(table, columns);

            var oldColumnIndex = columns.FindIndex(col => col.Name == tempColumn);
            var newColumnIndex = columns.FindIndex(col => col.Name == column);

            var source = new DBSource(tempTable);
            Func<string[], string[]> rowTransFunc = arr =>
            {
                arr[newColumnIndex] = transform(arr[oldColumnIndex]);
                return arr;
            };
            var trans = new RowTransformation(rowTransFunc);
            var dest = new DBDestination(table);

            source.LinkTo(trans);
            trans.LinkTo(dest);

            source.Execute();
            dest.Wait();

            int rowCount = RowCountTask.Count(table).Value;
            // TODO: stats on updates

            // Log information about the result of the trasnformation
            var countTask = new RowCountTask(table, string.Format("{0} != {1} or ({0} is NULL and {1} is not NULL) or ({0} is not NULL and {1} is NULL)", tempColumn, column));
            EtlFlowLogger.Info("{0} rows have been updated", countTask.Count().Rows);

            string[] curCol = null;
            var examples = new List<string[]>();
            var sql = string.Format("select distinct {0}, {1} from {2} where {0} != {1} or ({0} is NULL and {1} is not NULL) or ({0} is not NULL and {1} is NULL)", column, tempColumn, table);
            var findExamplesTask = new SqlTask("Select a few examples",
                sql, 
                () => {
                    curCol = new string[2];
                }, 
                () => {
                    examples.Add(curCol);
                },
                col => curCol[1] = col != null ? col.ToString(): string.Empty,
                tempCol => curCol[0] = tempCol.ToString())
            {
                
                ReadTopX = 5
            };
            findExamplesTask.ExecuteReader();
            foreach (var example in examples)
            {
                EtlFlowLogger.Info("{0} => {1}", example[0], example[1]);
            }

            // Cleanup behind by dropping the temp table and column created
            DropTableTask.Drop(tempTable);
            var dropTempCol = new SqlTask("Drop temp column", string.Format("ALTER TABLE [{0}] DROP COLUMN [{1}]", table, tempColumn));
            dropTempCol.Execute();
            EtlFlowLogger.Info("End of execution of TransformColumn task");

            return table;
        }


        /// <summary>
        /// Copies the source database table, and all its content, to the target table.
        /// The two tables can be in different databases.
        /// /!\ Only works with nvarchar columns for the moment.
        /// </summary>
        private static void CopyTable(string srcConnectionString, string srcDbName, string srcTable, 
            string tgtConnectionString, string tgtDbName, string tgtTable)
        {
            // Retrieve columns in source table
            var columns = GetTableColumns(srcConnectionString, srcDbName, srcTable);

            // Create target table with the columns in the source table (drop the table before if it exists)
            var tgtConnectionManager = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", tgtConnectionString, tgtDbName)));
            ControlFlow.CurrentDbConnection = tgtConnectionManager;
            DropTableTask.Drop(tgtTable);
            CreateTableTask.Create(tgtTable, columns);

            // Create a basic transformation (row => row) from the source to the target
            var srcConnectionManager = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", srcConnectionString, srcDbName)));
            DBSource source = new DBSource(srcConnectionManager, srcTable);
            RowTransformation trans = new RowTransformation(row =>
            {
                return row;
            });
            DBDestination destination = new DBDestination(tgtConnectionManager, tgtTable);

            source.LinkTo(trans);
            trans.LinkTo(destination);
            source.Execute();
            destination.Wait();
        }

        /// <summary>
        /// Pivot a SQL table of raw infobox properties (one row per property) to SQL table
        /// of company (one row per company/infobox).
        /// This code is very specific to the SQL table and cannot be reused for tables with a different schema.
        /// </summary>
        private static void PivotProperties(string connectionString, string dbName, string srcTable, string tgtTable)
        {
            // Create control flow
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(connectionString));

            // Create database
            CreateDatabaseTask.Create(dbName);

            // Create table for Forbes 2018 company data
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, dbName)));

            // Retrieve the list of all the property keys
            var source = new DBSource<PropKeyName>() {
                Sql = string.Format(@"select PropKey from {0} group by PropKey", srcTable)
            };

            var propertyKeys = new List<PropKeyName>();
            var dest = new CustomDestination<PropKeyName>(
                row => {
                    propertyKeys.Add(row);
                }
            );

            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            // Then create the target table with those keys as columns
            var tableColumns = propertyKeys
                .Where(key => Regex.IsMatch(key.PropKey, @"^[\w_]+$"))
                .Select(key => new TableColumn(key.PropKey, "nvarchar(max)", allowNulls: true))
                .ToList();
            tableColumns.Insert(0, new TableColumn("PageTitle", "nvarchar(max)", allowNulls: true));
            //tableColumns.Insert(0, new TableColumn("ID", "int", allowNulls: false, isPrimaryKey: true, isIdentity: true));

            // Copy the table
            DropTableTask.Drop(tgtTable);
            CreateTableTask.Create(tgtTable, tableColumns);

            var sqlPivotQuery = string.Format(@"DECLARE @colsPivot NVARCHAR(max),
	@query NVARCHAR(max);

select @colsPivot = STUFF((SELECT  ',' 
                      + quotename(PropKey)
					from {0}
					where PropKey is not NULL
					group by PropKey
            FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)') 
        ,1,1,'')

set @query = 
'INSERT INTO {1} (PageTitle, ' + @colsPivot + ')
select PageTitle, ' + @colsPivot + '
from (
	select PropKey, PropValue, InfoboxId, PageTitle
	from {0}
) as Props
PIVOT (MIN(PropValue)
	FOR PropKey IN (' + @colsPivot + '))
as PVT'

exec(@query)", srcTable, tgtTable);
            SqlTask.ExecuteNonQuery("Pivot properties", sqlPivotQuery);

            int rowCount = RowCountTask.Count(tgtTable).Value;
            Console.WriteLine("Inserted {0} rows in table '{1}'", rowCount, tgtTable);
        }


        // Forbes data ETL ------------------------------------------------------------------------------------------------------

        public static void ForbesDataEtl()
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json");
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            var connectionString = config["connectionString"];

            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var dbName = string.Format("tucdb_{0}", timestamp);

            var csvFilePath = "C:/Users/Alex/Documents/Github/WikimediaParser/CompanyScrapper/Results/forbes-top-2000-companies-2018.csv";

            ForbesCsvToRaw(connectionString, dbName, csvFilePath, "dbo.Forbes2018Raw");
            Console.WriteLine("=======================");
            ForbesRawToCompanyDb(connectionString, dbName, "dbo.Forbes2018Raw", "dbo.Forbes2018");
        }

        private static void ForbesCsvToRaw(string connectionString, string dbName, string csvFilePath, string tgtTable)
        {            
            // Create control flow
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(connectionString);

            // Create database
            CreateDatabaseTask.Create(dbName);

            // Create table for Forbes 2018 company data
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, dbName)));
            CreateTableTask.Create(tgtTable, new List<TableColumn>()
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
            CSVSource source = new CSVSource(csvFilePath);
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
            DBDestination<ForbesCompanyData> dest = new DBDestination<ForbesCompanyData>(tgtTable);

            source.LinkTo(row);
            row.LinkTo(dest);

            source.Execute();
            dest.Wait();

            int rowCount = RowCountTask.Count(tgtTable).Value;
            Console.WriteLine("Inserted {0} rows in {1}", rowCount, tgtTable);
        }

        private static void ForbesRawToCompanyDb(string connectionString, string dbName, string srcTable, string tgtTable)
        {
            // Create control flow
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(connectionString));

            // Create database
            CreateDatabaseTask.Create(dbName);

            // Create table for Forbes 2018 company data
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(string.Format("{0};Initial Catalog={1}", connectionString, dbName)));

            CreateTableTask.Create(tgtTable, new List<TableColumn>()
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
            
            var source = new DBSource<ForbesCompanyData>(string.Format(@"
                select ID, Name, Rank, Position, Uri, ImageUri, Industry, Country, Revenue, MarketValue, Headquarters, Ceo, Profits, Assets, State, SquareImage, Thumbnail 
                from {0}", srcTable));
            var trans = new RowTransformation<ForbesCompanyData, CompanyData>(
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
            var dest = new DBDestination<CompanyData>(tgtTable);

            source.LinkTo(trans);
            trans.LinkTo(dest);

            source.Execute();
            dest.Wait();

            int rowCount = RowCountTask.Count(tgtTable).Value;
            Console.WriteLine("Inserted {0} rows in table '{1}'", rowCount, tgtTable);
        }

    }
}
