using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the fall of wickets during an innings of a match
    /// </summary>
    public partial class FallOfWicketAddTable : MigrationBase
    {
        public FallOfWicketAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<FallOfWicketAddTable>("Running migration {MigrationStep}", typeof(FallOfWicketAddTable).Name);

            if (TableExists(Tables.FallOfWicket) == false)
            {
                Create.Table<FallOfWicketTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<FallOfWicketAddTable>("The database table {DbTable} already exists, skipping", Tables.FallOfWicket);
            }
        }
    }
}