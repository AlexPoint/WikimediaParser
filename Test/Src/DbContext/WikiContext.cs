using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Src.DbContext;

namespace WikitionaryDumpParser.Src.DbContext
{
    public class WikiContext : System.Data.Entity.DbContext
    {

        public DbSet<Infobox> Infoboxes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Con
            base.OnModelCreating(modelBuilder);
            // @"Server=.\SQLEXPRESS;Database=SchoolDB;Trusted_Connection=True;"
        }

    }
}
