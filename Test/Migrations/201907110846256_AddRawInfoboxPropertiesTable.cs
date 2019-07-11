namespace Test.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRawInfoboxPropertiesTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RawInfoboxProperties",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PageTitle = c.String(),
                        Key = c.String(),
                        Value = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.RawInfoboxProperties");
        }
    }
}
