using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording the batting performances in stoolball matches
    /// </summary>
    public partial class PlayerInningsAddTable : MigrationBase
    {
        public PlayerInningsAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<PlayerInningsAddTable>("Running migration {MigrationStep}", typeof(PlayerInningsAddTable).Name);

            if (TableExists(Constants.Tables.PlayerInnings) == false)
            {
                Create.Table<PlayerInningsTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<PlayerInningsAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.PlayerInnings);
            }
        }
    }
}