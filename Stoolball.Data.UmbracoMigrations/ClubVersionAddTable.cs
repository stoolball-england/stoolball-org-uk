using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball clubs which may change over time
    /// </summary>
    public partial class ClubVersionAddTable : MigrationBase
    {
        public ClubVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(ClubVersionAddTable).Name);

            if (TableExists(Tables.ClubVersion) == false)
            {
                Create.Table<ClubVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.ClubVersion);
            }
        }
    }
}