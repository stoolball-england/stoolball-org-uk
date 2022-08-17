using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

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

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(MatchLocationAddTable).Name);

            if (TableExists(Tables.MatchLocation) == false)
            {
                Create.Table<MatchLocationTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.MatchLocation);
            }
        }
    }
}