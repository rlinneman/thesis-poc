namespace Rel.Data.Ef6.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Asset",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        JobId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        ServiceArea = c.String(nullable: false, maxLength: 100),
                        PercentTolerance = c.Double(),
                        StaticTolerance = c.Double(),
                        MonotonicTolerance = c.Double(),
                        MaximumAndMinimumDecay = c.Double(),
                        MaxMinDecayWithStepAndTol = c.Double(),
                        MinimumDecay = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Job", t => t.JobId, cascadeDelete: true)
                .Index(t => new { t.JobId, t.Name }, unique: true, name: "UQ_AssetName");
            
            CreateTable(
                "dbo.Job",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        City = c.String(maxLength: 250),
                        LockedBy = c.String(maxLength: 20),
                        LockedOn = c.DateTime(),
                        Name = c.String(nullable: false, maxLength: 100),
                        PostalCode = c.String(maxLength: 10),
                        State = c.String(maxLength: 2),
                        Street1 = c.String(maxLength: 200),
                        Street2 = c.String(maxLength: 100),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "UQ_JobName");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Asset", "JobId", "dbo.Job");
            DropIndex("dbo.Job", "UQ_JobName");
            DropIndex("dbo.Asset", "UQ_AssetName");
            DropTable("dbo.Job");
            DropTable("dbo.Asset");
        }
    }
}
