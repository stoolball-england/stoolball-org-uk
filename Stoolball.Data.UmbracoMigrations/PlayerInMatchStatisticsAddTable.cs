using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording derived data about players' performances in stoolball matches
    /// </summary>
    public partial class PlayerInMatchStatisticsAddTable : MigrationBase
    {
        public PlayerInMatchStatisticsAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(PlayerInMatchStatisticsAddTable).Name);

            if (TableExists(Tables.PlayerInMatchStatistics) == false)
            {
                Create.Table<PlayerInMatchStatisticsTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.PlayerInMatchStatistics);
            }
        }
    }
}