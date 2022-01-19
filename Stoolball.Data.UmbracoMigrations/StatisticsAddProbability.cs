using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the home grounds and venues of stoolball teams which can change over time
    /// </summary>
    public partial class StatisticsAddProbability : MigrationBase
    {
        public StatisticsAddProbability(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(StatisticsAddProbability).Name);

            if (!ColumnExists(Tables.PlayerInMatchStatistics, "Probability"))
            {
                Create.Column("Probability").OnTable(Tables.PlayerInMatchStatistics).AsInt32().Nullable().Do();
                Execute.Sql($@"
CREATE OR ALTER PROCEDURE usp_Statistics_UpdateProbability
	@@TeamId uniqueidentifier NULL AS BEGIN
	SET NOCOUNT ON;

	DECLARE @@Probability TABLE (PlayerIdentityId uniqueidentifier, Probability int)

	IF @@TeamId IS NULL
		INSERT INTO @@Probability
		SELECT stats.PlayerIdentityId,
		COUNT(DISTINCT MatchId) -(SELECT COUNT(DISTINCT MatchId) * 5 FROM {Tables.PlayerInMatchStatistics} WHERE TeamId = stats.TeamId AND MatchStartTime > MAX(stats.MatchStartTime)) AS Probability
		FROM {Tables.PlayerInMatchStatistics} AS stats 
		GROUP BY stats.PlayerIdentityId, stats.PlayerIdentityName,stats.TeamId
	ELSE
		INSERT INTO @@Probability
		SELECT stats.PlayerIdentityId,
		COUNT(DISTINCT MatchId) -(SELECT COUNT(DISTINCT MatchId) * 5 FROM {Tables.PlayerInMatchStatistics} WHERE TeamId = stats.TeamId AND MatchStartTime > MAX(stats.MatchStartTime)) AS Probability
		FROM {Tables.PlayerInMatchStatistics} AS stats 
		WHERE stats.TeamId = @@TeamId
		GROUP BY stats.PlayerIdentityId, stats.PlayerIdentityName,stats.TeamId

	UPDATE {Tables.PlayerInMatchStatistics}
	SET {Tables.PlayerInMatchStatistics}.Probability = Probability.Probability
	FROM @@Probability Probability
	WHERE Probability.PlayerIdentityId = {Tables.PlayerInMatchStatistics}.PlayerIdentityId END
").Do();
            }
            else
            {
                Logger.LogDebug("The database column {DbTable}.{DbColumn} already exists, skipping", Tables.PlayerInMatchStatistics, "Probability");
            }
        }
    }
}