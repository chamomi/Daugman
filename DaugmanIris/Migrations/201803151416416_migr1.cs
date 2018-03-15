namespace DaugmanIris.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class migr1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MyImages", "IrisX", c => c.Int(nullable: false));
            AddColumn("dbo.MyImages", "IrisY", c => c.Int(nullable: false));
            AddColumn("dbo.MyImages", "IrisR", c => c.Int(nullable: false));
            AddColumn("dbo.MyImages", "PupilX", c => c.Int(nullable: false));
            AddColumn("dbo.MyImages", "PupilY", c => c.Int(nullable: false));
            AddColumn("dbo.MyImages", "PupilR", c => c.Int(nullable: false));
            DropColumn("dbo.MyImages", "Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.MyImages", "Id", c => c.Int(nullable: false));
            DropColumn("dbo.MyImages", "PupilR");
            DropColumn("dbo.MyImages", "PupilY");
            DropColumn("dbo.MyImages", "PupilX");
            DropColumn("dbo.MyImages", "IrisR");
            DropColumn("dbo.MyImages", "IrisY");
            DropColumn("dbo.MyImages", "IrisX");
        }
    }
}
