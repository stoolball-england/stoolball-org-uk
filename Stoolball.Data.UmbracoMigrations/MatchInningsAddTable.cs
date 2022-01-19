using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording team innings in stoolball matches
    /// </summary>
    public partial class MatchInningsAddTable : MigrationBase
    {
        public MatchInningsAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(MatchInningsAddTable).Name);

            if (TableExists(Tables.MatchInnings) == false)
            {
                Create.Table<MatchInningsTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.MatchInnings);
            }
        }
    }
}