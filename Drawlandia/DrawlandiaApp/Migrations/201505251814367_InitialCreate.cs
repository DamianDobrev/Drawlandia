//namespace DrawlandiaApp.Migrations
//{
//    using System;
//    using System.Data.Entity.Migrations;
    
//    public partial class InitialCreate : DbMigration
//    {
//        public override void Up()
//        {
//            CreateTable(
//                "dbo.Players",
//                c => new
//                    {
//                        Id = c.Int(nullable: false, identity: true),
//                        ConnectionId = c.String(),
//                        Name = c.String(),
//                        RoomId = c.Int(nullable: false),
//                        State = c.Int(nullable: false),
//                        Score = c.Int(nullable: false),
//                        IsHisTurn = c.Boolean(nullable: false),
//                    })
//                .PrimaryKey(t => t.Id)
//                .ForeignKey("dbo.Rooms", t => t.RoomId, cascadeDelete: true)
//                .Index(t => t.RoomId);
            
//            CreateTable(
//                "dbo.Rooms",
//                c => new
//                    {
//                        Id = c.Int(nullable: false, identity: true),
//                        Name = c.String(),
//                        Password = c.String(),
//                        State = c.Int(nullable: false),
//                    })
//                .PrimaryKey(t => t.Id);
            
//            CreateTable(
//                "dbo.Words",
//                c => new
//                    {
//                        Id = c.Int(nullable: false, identity: true),
//                        WordText = c.String(),
//                    })
//                .PrimaryKey(t => t.Id);
            
//        }
        
//        public override void Down()
//        {
//            DropForeignKey("dbo.Players", "RoomId", "dbo.Rooms");
//            DropIndex("dbo.Players", new[] { "RoomId" });
//            DropTable("dbo.Words");
//            DropTable("dbo.Rooms");
//            DropTable("dbo.Players");
//        }
//    }
//}
