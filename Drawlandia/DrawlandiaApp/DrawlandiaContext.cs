using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DrawlandiaApp.Models;
using DrawlandiaApp.Migrations;

namespace DrawlandiaApp
{
    public class DrawlandiaContext : DbContext
    {
        public DrawlandiaContext()
            : base("Drawlandia")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DrawlandiaContext, Configuration>());
        }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<Player> Players { get; set; }

        public DbSet<Word> Words { get; set; }
    }
}