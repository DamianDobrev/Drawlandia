using System.Data.Entity;
using DrawlandiaApp.Models;

namespace DrawlandiaApp
{
    public class DrawlandiaContext : DbContext
    {
        public DrawlandiaContext()
            : base("Drawlandia")
        {
        }

        public DbSet<Game> Games { get; set; }

        public DbSet<Player> Players { get; set; }

        public DbSet<Word> Words { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
                        .HasRequired(a => a.CurrentGame)
                        .WithMany()
                        .HasForeignKey(u => u.CurrentGameId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Player>()
                        .HasRequired<Game>(g => g.CurrentGame)
                        .WithMany(s => s.Players)
                        .HasForeignKey(s => s.CurrentGameId).WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}