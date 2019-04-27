namespace Test.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddInfoboxTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Infoboxes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PageTitle = c.String(),
                        PageId = c.Int(nullable: false),
                        RawText = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Infoboxes");
        }
    }
}
