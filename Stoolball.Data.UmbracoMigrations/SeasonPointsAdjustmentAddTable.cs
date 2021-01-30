using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording what bonus or penalty points are awarded or deducted in stoolball seasons
    /// </summary>
    public partial class SeasonPointsAdjustmentAddTable : MigrationBase
    {
        public SeasonPointsAdjustmentAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonPointsAdjustmentAddTable>("Running migration {MigrationStep}", typeof(SeasonPointsAdjustmentAddTable).Name);

            if (TableExists(Tables.SeasonPointsAdjustment) == false)
            {
                Create.Table<SeasonPointsAdjustmentTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonPointsAdjustmentAddTable>("The database table {DbTable} already exists, skipping", Tables.SeasonPointsAdjustment);
            }
        }
    }
}