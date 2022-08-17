using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Removes the probability column and the stored procedure to populate it. Probability is now calculated differently as part of the SELECT.
    /// </summary>
    public partial class StatisticsRemoveProbability : MigrationBase
    {
        public StatisticsRemoveProbability(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(StatisticsRemoveProbability).Name);

            if (ColumnExists(Tables.PlayerInMatchStatistics, "Probability"))
            {
                Delete.Column("Probability").FromTable(Tables.PlayerInMatchStatistics).Do();
                Execute.Sql("DROP PROCEDURE IF EXISTS usp_Statistics_UpdateProbability").Do();
            }
            else
            {
                Logger.LogDebug("The database column {DbTable}.{DbColumn} does not exist, skipping", Tables.PlayerInMatchStatistics, "Probability");
            }
        }
    }
}