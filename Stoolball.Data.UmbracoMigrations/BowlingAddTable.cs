using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording bowling statistics for a player identity in stoolball matches
    /// </summary>
    public partial class BowlingAddTable : MigrationBase
    {
        public BowlingAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<BowlingAddTable>("Running migration {MigrationStep}", typeof(BowlingAddTable).Name);

            if (TableExists(Constants.Tables.Bowling) == false)
            {
                Create.Table<BowlingTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<BowlingAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Bowling);
            }
        }
    }
}