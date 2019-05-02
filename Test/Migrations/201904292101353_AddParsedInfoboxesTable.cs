namespace Test.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddParsedInfoboxesTable : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.InfoboxProperties", "Infobox_Id", "dbo.Infoboxes");
            DropIndex("dbo.InfoboxProperties", new[] { "Infobox_Id" });
            CreateTable(
                "dbo.ParsedInfoboxes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ArticleUrl = c.String(),
                        Html = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.InfoboxProperties", "Property", c => c.String());
            AddColumn("dbo.InfoboxProperties", "ParsedInfobox_Id", c => c.Int());
            CreateIndex("dbo.InfoboxProperties", "ParsedInfobox_Id");
            AddForeignKey("dbo.InfoboxProperties", "ParsedInfobox_Id", "dbo.ParsedInfoboxes", "Id");
            DropColumn("dbo.InfoboxProperties", "RawText");
            DropColumn("dbo.InfoboxProperties", "Key");
            DropColumn("dbo.InfoboxProperties", "Infobox_Id");
            DropTable("dbo.Infoboxes");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.Infoboxes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PageTitle = c.String(),
                        PageId = c.Int(nullable: false),
                        RawText = c.String(),
                        Template = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.InfoboxProperties", "Infobox_Id", c => c.Int());
            AddColumn("dbo.InfoboxProperties", "Key", c => c.String());
            AddColumn("dbo.InfoboxProperties", "RawText", c => c.String());
            DropForeignKey("dbo.InfoboxProperties", "ParsedInfobox_Id", "dbo.ParsedInfoboxes");
            DropIndex("dbo.InfoboxProperties", new[] { "ParsedInfobox_Id" });
            DropColumn("dbo.InfoboxProperties", "ParsedInfobox_Id");
            DropColumn("dbo.InfoboxProperties", "Property");
            DropTable("dbo.ParsedInfoboxes");
            CreateIndex("dbo.InfoboxProperties", "Infobox_Id");
            AddForeignKey("dbo.InfoboxProperties", "Infobox_Id", "dbo.Infoboxes", "Id");
        }
    }
}
