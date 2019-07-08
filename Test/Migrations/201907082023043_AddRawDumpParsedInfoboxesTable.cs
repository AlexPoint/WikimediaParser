namespace Test.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRawDumpParsedInfoboxesTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RawDumpParsedInfoboxes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PageTitle = c.String(),
                        Markdown = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.RawDumpParsedInfoboxes");
        }
    }
}
