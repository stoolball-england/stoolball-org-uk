using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

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

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(PointsAdjustmentAddTable).Name);

            if (TableExists(Tables.PointsAdjustment) == false)
            {
                Create.Table<PointsAdjustmentTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.PointsAdjustment);
            }
        }
    }
}