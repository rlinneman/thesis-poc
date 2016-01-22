namespace Rel.Data.Ef6.Migrations
{
    using Rel.Data.Models;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Rel.Data.Ef6.TpContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(Rel.Data.Ef6.TpContext context)
        {
            // This method will be called after migrating to the
            // latest version.

            // You can use the DbSet<T>.AddOrUpdate() helper extension
            // method to avoid creating duplicate seed data. E.g.
            // 
            // context.People.AddOrUpdate( entity => entity.FullName,
            // new Person { FullName = "Andrew Peters" }, new Person {
            // FullName = "Brice Lambson" }, new Person { FullName =
            // "Rowan Miller" } );

            var jobs = new Job[]
            {
                new Job{ Id=1 , Name="Empire State"                 , Street1="350 5th Ave"         , City="New York"   , State="NY", PostalCode="10118" },
                new Job{ Id=2 , Name="Australia Bike Retailer"      , Street1="5672 Hale Dr."       , City="Bothell"    , State="WA", PostalCode="98011" },
                //new Job{ Id=3 , Name="Allenson Cycles"              , Street1="1399 Firestone Drive", City="Bothell"    , State="WA", PostalCode="98011" },
                //new Job{ Id=4 , Name="Tacoma Narrows"               , Street1="16 HWY"              , City="Tacoma"     , State="WA", PostalCode="98406", LockedBy="bbunny", LockedOn=new DateTime(2015,11,25) },
                //new Job{ Id=5 , Name="Flarsheim Annex"              , Street1="5100 Rockhill RD"    , City="Kansas City", State="MO", PostalCode="64110", LockedBy="ryan"  , LockedOn=new DateTime(2015,11,29) },
                //new Job{ Id=6 , Name="Trikes, Inc."                 , Street1="1226 Shoe St."       , City="Bothell"    , State="WA", PostalCode="98011" },
                //new Job{ Id=7 , Name="Morgan Bike Accessories"      , Street1="9539 Glenside Dr"    , City="Bothell"    , State="WA", PostalCode="98011" },
                //new Job{ Id=8 , Name="Cycling Master"               , Street1="7484 Roundtree Drive", City="Bothell"    , State="WA", PostalCode="98011" },
                //new Job{ Id=9 , Name="Chicago Rent-All"             , Street1="9833 Mt. Dias Blv."  , City="Bothell"    , State="WA", PostalCode="98011" },
                //new Job{ Id=10, Name="Greenwood Athletic Company"   , Street1="1970 Napa Ct."       , City="Bothell"    , State="WA", PostalCode="98011" },
            };
            context.Jobs.AddOrUpdate(_ => _.Name, jobs);

            //context.Assets.AddOrUpdate(jobs.SelectMany(_ => AssetGenerator.CreateAssets(_.Id, 5000)).ToArray());
        }
    }
}