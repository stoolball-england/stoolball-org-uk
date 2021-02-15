using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

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

        public override void Migrate()
        {
            Logger.Debug<PlayerInMatchStatisticsAddTable>("Running migration {MigrationStep}", typeof(PlayerInMatchStatisticsAddTable).Name);

            if (TableExists(Tables.PlayerInMatchStatistics) == false)
            {
                Create.Table<PlayerInMatchStatisticsTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<PlayerInMatchStatisticsAddTable>("The database table {DbTable} already exists, skipping", Tables.PlayerInMatchStatistics);
            }
        }
    }
}