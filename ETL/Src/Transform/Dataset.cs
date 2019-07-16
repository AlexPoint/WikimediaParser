using System;
using System.Collections.Generic;
using System.Text;

namespace ETL.Src.Transform
{
    class Dataset
    {
        public string[] Columns { get; set; }
        public List<string[]> Data { get; set; }
    }
}
