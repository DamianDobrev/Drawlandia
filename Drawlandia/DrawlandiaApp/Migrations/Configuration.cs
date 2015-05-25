namespace DrawlandiaApp.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<DrawlandiaApp.DrawlandiaContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "DrawlandiaApp.DrawlandiaContext";
        }

        protected override void Seed(DrawlandiaApp.DrawlandiaContext context)
        {
        }
    }
}
