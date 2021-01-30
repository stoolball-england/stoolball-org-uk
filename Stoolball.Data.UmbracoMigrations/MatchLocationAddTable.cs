using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball grounds and indoor venues
    /// </summary>
    public partial class MatchLocationAddTable : MigrationBase
    {
        public MatchLocationAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchLocationAddTable>("Running migration {MigrationStep}", typeof(MatchLocationAddTable).Name);

            if (TableExists(Tables.MatchLocation) == false)
            {
                Create.Table<MatchLocationTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchLocationAddTable>("The database table {DbTable} already exists, skipping", Tables.MatchLocation);
            }
        }
    }
}