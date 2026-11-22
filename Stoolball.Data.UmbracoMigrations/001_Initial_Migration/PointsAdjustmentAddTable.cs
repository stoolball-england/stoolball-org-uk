using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording what bonus or penalty points are awarded or deducted in a league format
    /// </summary>
    public partial class PointsAdjustmentAddTable : AsyncMigrationBase
    {
        public PointsAdjustmentAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
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

            return Task.CompletedTask;
        }
    }
}