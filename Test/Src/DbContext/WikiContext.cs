using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Src;
using Test.Src.DbContext;

namespace WikitionaryDumpParser.Src.DbContext
{
    public class WikiContext : System.Data.Entity.DbContext
    {

        /*public DbSet<Infobox> Infoboxes { get; set; }
        public DbSet<Test.Src.DbContext.InfoboxProperty> InfoboxProperties { get; set; }*/
        public DbSet<ParsedInfobox> ParsedInfoboxes { get; set; }

        public DbSet<RawDumpParsedInfobox> RawDumpParsedInfoboxes { get; set; }
        public DbSet<RawInfoboxProperty> RawInfoboxProperties { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
