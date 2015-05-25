
using System.Data.Entity;
using DbInitializer.Models;

namespace DbInitializer
{
    public class DrawlandiaContext : DbContext
    {
        public DrawlandiaContext() : base("Drawlandia")
        {
        }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<Player> Players { get; set; }

        public DbSet<Word> Words { get; set; }
    }
}