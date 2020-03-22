using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Migrations
{
    /// <summary>
    /// Adds a table for recording the batting performances in stoolball matches
    /// </summary>
    public partial class BattingAddTable : MigrationBase
    {
        public BattingAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<BattingAddTable>("Running migration {MigrationStep}", typeof(BattingAddTable).Name);

            if (TableExists(Constants.Tables.Batting) == false)
            {
                Create.Table<BattingTableSchema>().Do();
            }
            else
            {
                Logger.Debug<BattingAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Batting);
            }
        }
    }
}