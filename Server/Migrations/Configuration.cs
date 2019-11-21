namespace Server.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<UserRegistration>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "Server.UserRegistration";
        }

        protected override void Seed(UserRegistration context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
