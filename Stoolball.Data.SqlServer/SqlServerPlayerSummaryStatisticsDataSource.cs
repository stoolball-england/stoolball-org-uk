using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets statistics about players performances (such as their overall average) from the Umbraco database
    /// </summary>
    public class SqlServerPlayerSummaryStatisticsDataSource : IPlayerSummaryStatisticsDataSource, ICacheablePlayerSummaryStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        public SqlServerPlayerSummaryStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory;
        }

        public async Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (filter?.Player?.PlayerId == null) { throw new ArgumentException("Player.PlayerId must be specified", nameof(filter)); }

            var sql = $@"SELECT TotalInnings, TotalInningsWithRunsScored, TotalInningsWithRunsScoredAndBallsFaced, NotOuts, TotalRunsScored, Fifties, Hundreds, BestInningsRunsScored, BestInningsWasDismissed, StrikeRate,
                         CASE WHEN DismissalsWithRunsScored > 0 THEN CAST(TotalRunsScored AS DECIMAL)/DismissalsWithRunsScored ELSE NULL END AS Average
                         FROM (
	                        SELECT 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType != {(int)DismissalType.DidNotBat}) AS TotalInnings,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL) AS TotalInningsWithRunsScored, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL AND BallsFaced IS NOT NULL) AS TotalInningsWithRunsScoredAndBallsFaced, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType IN ({(int)DismissalType.NotOut},{(int)DismissalType.Retired},{(int)DismissalType.RetiredHurt})) AS NotOuts, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND PlayerWasDismissed = 1 AND RunsScored IS NOT NULL) AS DismissalsWithRunsScored, 
		                        (SELECT SUM(RunsScored) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId =  @PlayerId AND RunsScored IS NOT NULL)  AS TotalRunsScored,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND RunsScored >= 50) AS Fifties,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND RunsScored >= 100) AS Hundreds,
		                        (SELECT CASE WHEN SUM(BallsFaced) > 0 THEN (CAST(SUM(RunsScored) AS DECIMAL)/SUM(BallsFaced))*100 ELSE NULL END 
                                        FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL AND BallsFaced IS NOT NULL) AS StrikeRate,
		                        (SELECT TOP 1 RunsScored FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId ORDER BY RunsScored DESC, PlayerWasDismissed ASC) AS BestInningsRunsScored,
		                        (SELECT TOP 1 PlayerWasDismissed FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId ORDER BY RunsScored DESC, PlayerWasDismissed ASC) AS BestInningsWasDismissed
	                     ) AS BattingStatistics";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<BattingStatistics>(sql, new
                {
                    filter.Player.PlayerId
                })
                .ConfigureAwait(false);
            }
        }

        public async Task<BowlingStatistics> ReadBowlingStatistics(StatisticsFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (filter?.Player?.PlayerId == null) { throw new ArgumentException("Player.PlayerId must be specified", nameof(filter)); }

            // A player can have multiple sets of bowling figures per innings if they have multiple identities in that innings,
            // which could happen if 'John Smith' was also entered as 'John', for example.
            // 
            // This query groups sets of bowling figures by MatchId and counts how many MatchInningsPairs there are, ie how many innings per match with bowling figures.
            // Because MatchInningsPair is not a unique value they have to be grouped by MatchId, which means one result per team per match, so SUM them to get a single total.
            var totalInningsSql = $@"SELECT SUM(BowlingFiguresPerInnings) FROM (
                                         SELECT COUNT(DISTINCT MatchInningsPair) AS BowlingFiguresPerInnings 
                                         FROM {Tables.PlayerInMatchStatistics} 
                                         WHERE PlayerId = @PlayerId AND BowlingFiguresId IS NOT NULL 
                                         GROUP BY MatchTeamId
                                    ) AS BowlingFiguresPerInnings";

            var totalInningsWithRunsSql = $@"SELECT SUM(BowlingFiguresPerInnings) FROM (
                                                SELECT COUNT(DISTINCT MatchInningsPair) AS BowlingFiguresPerInnings 
                                                FROM {Tables.PlayerInMatchStatistics} 
                                                WHERE PlayerId = @PlayerId AND BowlingFiguresId IS NOT NULL AND RunsConceded IS NOT NULL
                                                GROUP BY MatchTeamId
                                        ) AS BowlingFiguresWithRunsPerInnings";

            var totalInningsWithBallsBowledSql = $@"SELECT SUM(BowlingFiguresPerInnings) FROM (
                                                SELECT COUNT(DISTINCT MatchInningsPair) AS BowlingFiguresPerInnings 
                                                FROM {Tables.PlayerInMatchStatistics} 
                                                WHERE PlayerId = @PlayerId AND BowlingFiguresId IS NOT NULL AND BallsBowled IS NOT NULL
                                                GROUP BY MatchTeamId
                                        ) AS BowlingFiguresWithOvers";

            var bestFiguresSql = $@"FROM {Tables.PlayerInMatchStatistics} 
								WHERE PlayerId = @PlayerId AND Wickets IS NOT NULL 
								GROUP BY MatchTeamId, MatchInningsPair 
								ORDER BY SUM(Wickets) DESC, CASE WHEN SUM(RunsConceded) IS NULL THEN 0 ELSE 1 END DESC, SUM(RunsConceded) ASC";


            var sql = $@"SELECT TotalInnings, TotalInningsWithRunsConceded, TotalInningsWithBallsBowled, TotalOvers, TotalMaidens, TotalRunsConceded, TotalWickets, 
                                FiveWicketInnings, BestInningsWickets, BestInningsRunsConceded, Economy, Average, StrikeRate
                         FROM (
	                        SELECT 
                                ({totalInningsSql}) AS TotalInnings,
                                ({totalInningsWithRunsSql}) AS TotalInningsWithRunsConceded,
                                ({totalInningsWithBallsBowledSql}) AS TotalInningsWithBallsBowled,
                                (SELECT SUM(BallsBowled)/{StatisticsConstants.BALLS_PER_OVER} + CAST((SUM(BallsBowled)%{StatisticsConstants.BALLS_PER_OVER})AS DECIMAL) / 10 FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId) AS TotalOvers,
                                (SELECT SUM(Maidens) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId) AS TotalMaidens,
                                (SELECT SUM(RunsConceded) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId) AS TotalRunsConceded,
                                (SELECT SUM(Wickets) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId) AS TotalWickets,
                                (SELECT COUNT(MatchTeamId) FROM (SELECT MatchTeamId FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId GROUP BY MatchTeamId, MatchInningsPair HAVING SUM(Wickets) >= 5) AS FiveWicketInnings) AS FiveWicketInnings,
                                (SELECT TOP 1 SUM(Wickets) {bestFiguresSql}) AS BestInningsWickets,
                                (SELECT TOP 1 SUM(RunsConceded) {bestFiguresSql}) AS BestInningsRunsConceded,
                                (SELECT CASE WHEN SUM(BallsBowled) > 0 THEN SUM(RunsConceded)/(CAST(SUM(BallsBowled) AS DECIMAL)/{StatisticsConstants.BALLS_PER_OVER}) ELSE NULL END FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND RunsConceded IS NOT NULL) AS Economy,
                                (SELECT CASE WHEN SUM(Wickets) > 0 THEN CAST(SUM(RunsConceded) AS DECIMAL)/SUM(Wickets) ELSE NULL END FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND RunsConceded IS NOT NULL) AS Average,
                                (SELECT CASE WHEN SUM(Wickets) > 0 THEN CAST(SUM(BallsBowled) AS DECIMAL)/SUM(Wickets) ELSE NULL END FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND BallsBowled IS NOT NULL) AS StrikeRate
	                     ) AS BowlingStatistics";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<BowlingStatistics>(sql, new
                {
                    filter.Player.PlayerId
                })
                .ConfigureAwait(false);
            }
        }

        public async Task<FieldingStatistics> ReadFieldingStatistics(StatisticsFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (filter?.Player?.PlayerId == null) { throw new ArgumentException("Player.PlayerId must be specified", nameof(filter)); }

            var sql = $@"SELECT TotalCatches, MostCatches, TotalRunOuts, MostRunOuts
                         FROM (
	                        SELECT 
                                (SELECT SUM(Catches) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId) AS TotalCatches,
                                (SELECT SUM(RunOuts) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId) AS TotalRunOuts,
                                (SELECT MAX(Catches) FROM (SELECT SUM(Catches) AS Catches FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId GROUP BY MatchTeamId, MatchInningsPair) AS CatchesPerInnings) AS MostCatches,
                                (SELECT MAX(RunOuts) FROM (SELECT SUM(RunOuts) AS RunOuts FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId GROUP BY MatchTeamId, MatchInningsPair) AS RunOutsPerInnings) AS MostRunOuts
	                     ) AS FieldingStatistics";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<FieldingStatistics>(sql, new
                {
                    filter.Player.PlayerId
                })
                .ConfigureAwait(false);
            }
        }
    }
}
