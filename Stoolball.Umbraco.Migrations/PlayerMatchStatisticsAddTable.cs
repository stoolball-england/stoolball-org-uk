using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Migrations
{
    /// <summary>
    /// Adds a table for recording derived data about players' performances in stoolball matches
    /// </summary>
    public partial class PlayerMatchStatisticsAddTable : MigrationBase
    {
        public PlayerMatchStatisticsAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<PlayerMatchStatisticsAddTable>("Running migration {MigrationStep}", typeof(PlayerMatchStatisticsAddTable).Name);

            if (TableExists(Constants.Tables.PlayerMatchStatistics) == false)
            {
                Create.Table<PlayerMatchStatisticsTableSchema>().Do();
            }
            else
            {
                Logger.Debug<PlayerMatchStatisticsAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.PlayerMatchStatistics);
            }
        }
    }
}