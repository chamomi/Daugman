namespace DaugmanIris.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MyImages",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 128),
                        Id = c.Int(nullable: false),
                        Image = c.Binary(),
                    })
                .PrimaryKey(t => t.Name);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.MyImages");
        }
    }
}
