using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the batting performances in stoolball matches
    /// </summary>
    public partial class PlayerInningsAddTable : MigrationBase
    {
        public PlayerInningsAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(PlayerInningsAddTable).Name);

            if (TableExists(Tables.PlayerInnings) == false)
            {
                Create.Table<PlayerInningsTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.PlayerInnings);
            }
        }
    }
}