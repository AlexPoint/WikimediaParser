namespace Test.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddReferenceToInfoboxIdInProperties : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RawInfoboxProperties", "InfoboxId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RawInfoboxProperties", "InfoboxId");
        }
    }
}
