using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETL.Src.Transform
{
    class Split
    {
        public string SrcColumn { get; set; }
        public string[] TgtColumns { get; set; }
        public Func<string, string[]> Transform { get; set; }


        public Dataset TransformDataset(Dataset input)
        {
            if(!input.Columns.Any(s => s == SrcColumn))
            {
                // silent failover
                // This operation has been badly configured, we just skip it.
                return input;
            }

            var result = new Dataset();

            // Careful addding new columns because we want to have unique column names and
            // the input and output should have the same column indices.
            result.Columns = input.Columns.ToArray();
            foreach(var tgtCol in TgtColumns)
            {
                if(!result.Columns.Any(s => s == tgtCol))
                {
                    result.Columns.Append(tgtCol);
                }
                // Otherwise the column already exists and there is no need to add it
            }

            int srcColumnIndex = Array.IndexOf<string>(result.Columns, SrcColumn);
            int[] tgtColumnIndices = TgtColumns.Select(col => Array.IndexOf<string>(result.Columns, col)).ToArray();
            result.Data = new List<string[]>();
            foreach(var row in input.Data)
            {
                var newValues = Transform(row[srcColumnIndex]);
                for(var i = 0; i < newValues.Length; i++)
                {
                    row[tgtColumnIndices[i]] = newValues[i];
                }
                result.Data.Add(row);
            }

            return result;
        }
    }
}
