namespace Test.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddInfoboxPropertiesTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.InfoboxProperties",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        RawText = c.String(),
                        Key = c.String(),
                        Value = c.String(),
                        Infobox_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Infoboxes", t => t.Infobox_Id)
                .Index(t => t.Infobox_Id);
            
            AddColumn("dbo.Infoboxes", "Template", c => c.String());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.InfoboxProperties", "Infobox_Id", "dbo.Infoboxes");
            DropIndex("dbo.InfoboxProperties", new[] { "Infobox_Id" });
            DropColumn("dbo.Infoboxes", "Template");
            DropTable("dbo.InfoboxProperties");
        }
    }
}
