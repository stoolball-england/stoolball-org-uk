using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording what points are awarded for match results in leagues
    /// </summary>
    public partial class PointsRuleAddTable : AsyncMigrationBase
    {
        public PointsRuleAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(PointsRuleAddTable).Name);

            if (TableExists(Tables.PointsRule) == false)
            {
                Create.Table<PointsRuleTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.PointsRule);
            }

            return Task.CompletedTask;
        }
    }
}