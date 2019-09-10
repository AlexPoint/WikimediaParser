namespace Test.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameKeyAndValueProperties : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RawInfoboxProperties", "PropKey", c => c.String());
            AddColumn("dbo.RawInfoboxProperties", "PropValue", c => c.String());
            DropColumn("dbo.RawInfoboxProperties", "Key");
            DropColumn("dbo.RawInfoboxProperties", "Value");
        }
        
        public override void Down()
        {
            AddColumn("dbo.RawInfoboxProperties", "Value", c => c.String());
            AddColumn("dbo.RawInfoboxProperties", "Key", c => c.String());
            DropColumn("dbo.RawInfoboxProperties", "PropValue");
            DropColumn("dbo.RawInfoboxProperties", "PropKey");
        }
    }
}
