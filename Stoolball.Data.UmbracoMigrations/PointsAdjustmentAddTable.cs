using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording what bonus or penalty points are awarded or deducted in a league format
    /// </summary>
    public partial class PointsAdjustmentAddTable : MigrationBase
    {
        public PointsAdjustmentAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<PointsAdjustmentAddTable>("Running migration {MigrationStep}", typeof(PointsAdjustmentAddTable).Name);

            if (TableExists(Tables.PointsAdjustment) == false)
            {
                Create.Table<PointsAdjustmentTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<PointsAdjustmentAddTable>("The database table {DbTable} already exists, skipping", Tables.PointsAdjustment);
            }
        }
    }
}