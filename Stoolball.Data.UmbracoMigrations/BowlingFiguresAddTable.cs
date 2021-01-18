using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording bowling statistics for a player identity in stoolball matches
    /// </summary>
    public partial class BowlingFiguresAddTable : MigrationBase
    {
        public BowlingFiguresAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<BowlingFiguresAddTable>("Running migration {MigrationStep}", typeof(BowlingFiguresAddTable).Name);

            if (TableExists(Constants.Tables.BowlingFigures) == false)
            {
                Create.Table<BowlingFiguresTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<BowlingFiguresAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.BowlingFigures);
            }
        }
    }
}