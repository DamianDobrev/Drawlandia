using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DrawlandiaApp.Models;

namespace DrawlandiaApp
{
    public class DrawlandiaContext : DbContext
    {
        public DrawlandiaContext()
            : base("Drawlandia")
        {
        }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<Player> Players { get; set; }

        public DbSet<Word> Words { get; set; }
    }
}