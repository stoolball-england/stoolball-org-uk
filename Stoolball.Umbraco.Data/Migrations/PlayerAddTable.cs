using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording players, who might play for different teams and have different names over time
    /// </summary>
    public partial class PlayerAddTable : MigrationBase
    {
        public PlayerAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<PlayerAddTable>("Running migration {MigrationStep}", typeof(PlayerAddTable).Name);

            if (TableExists(Constants.Tables.Player) == false)
            {
                Create.Table<PlayerTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<PlayerAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Player);
            }
        }
    }
}