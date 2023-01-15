using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.Abstractions;
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
        private readonly IStatisticsQueryBuilder _statisticsQueryBuilder;

        public SqlServerPlayerSummaryStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IStatisticsQueryBuilder statisticsQueryBuilder)
        {
            _databaseConnectionFactory = databaseConnectionFactory;
            _statisticsQueryBuilder = statisticsQueryBuilder ?? throw new ArgumentNullException(nameof(statisticsQueryBuilder));
        }

        public async Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (filter?.Player?.PlayerId == null) { throw new ArgumentException("Player.PlayerId must be specified", nameof(filter)); }

            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);

            var sql = $@"SELECT TotalInnings, TotalInningsWithRunsScored, TotalInningsWithRunsScoredAndBallsFaced, NotOuts, TotalRunsScored, Fifties, Hundreds, BestInningsRunsScored, BestInningsWasDismissed, StrikeRate,
                         CASE WHEN DismissalsWithRunsScored > 0 THEN CAST(TotalRunsScored AS DECIMAL)/DismissalsWithRunsScored ELSE NULL END AS Average
                         FROM (
	                        SELECT 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE DismissalType != {(int)DismissalType.DidNotBat} {where}) AS TotalInnings,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL {where}) AS TotalInningsWithRunsScored, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL AND BallsFaced IS NOT NULL {where}) AS TotalInningsWithRunsScoredAndBallsFaced, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE DismissalType IN ({(int)DismissalType.NotOut},{(int)DismissalType.Retired},{(int)DismissalType.RetiredHurt}) {where}) AS NotOuts, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerWasDismissed = 1 AND RunsScored IS NOT NULL {where}) AS DismissalsWithRunsScored, 
		                        (SELECT SUM(RunsScored) FROM {Tables.PlayerInMatchStatistics} WHERE RunsScored IS NOT NULL {where})  AS TotalRunsScored,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE RunsScored >= 50 {where}) AS Fifties,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE RunsScored >= 100 {where}) AS Hundreds,
		                        (SELECT CASE WHEN SUM(BallsFaced) > 0 THEN (CAST(SUM(RunsScored) AS DECIMAL)/SUM(BallsFaced))*100 ELSE NULL END 
                                        FROM {Tables.PlayerInMatchStatistics} WHERE DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL AND BallsFaced IS NOT NULL {where}) AS StrikeRate,
		                        (SELECT TOP 1 RunsScored FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where} ORDER BY RunsScored DESC, PlayerWasDismissed ASC) AS BestInningsRunsScored,
		                        (SELECT TOP 1 PlayerWasDismissed FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where} ORDER BY RunsScored DESC, PlayerWasDismissed ASC) AS BestInningsWasDismissed
	                     ) AS BattingStatistics";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<BattingStatistics>(sql, parameters)
                .ConfigureAwait(false);
            }
        }

        public async Task<BowlingStatistics> ReadBowlingStatistics(StatisticsFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (filter?.Player?.PlayerId == null) { throw new ArgumentException("Player.PlayerId must be specified", nameof(filter)); }

            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);

            var totalInningsSql = $@"SELECT SUM(BowlingFiguresPerInnings) FROM (
                                         SELECT COUNT(DISTINCT MatchInningsPair) AS BowlingFiguresPerInnings 
                                         FROM {Tables.PlayerInMatchStatistics} 
                                         WHERE BowlingFiguresId IS NOT NULL {where}
                                         GROUP BY MatchTeamId
                                    ) AS BowlingFiguresPerInnings";

            var totalInningsWithRunsSql = $@"SELECT SUM(BowlingFiguresPerInnings) FROM (
                                                SELECT COUNT(DISTINCT MatchInningsPair) AS BowlingFiguresPerInnings 
                                                FROM {Tables.PlayerInMatchStatistics} 
                                                WHERE BowlingFiguresId IS NOT NULL AND RunsConceded IS NOT NULL {where}
                                                GROUP BY MatchTeamId
                                        ) AS BowlingFiguresWithRunsPerInnings";

            var totalInningsWithBallsBowledSql = $@"SELECT SUM(BowlingFiguresPerInnings) FROM (
                                                SELECT COUNT(DISTINCT MatchInningsPair) AS BowlingFiguresPerInnings 
                                                FROM {Tables.PlayerInMatchStatistics} 
                                                WHERE BowlingFiguresId IS NOT NULL AND BallsBowled IS NOT NULL {where}
                                                GROUP BY MatchTeamId
                                        ) AS BowlingFiguresWithOvers";

            // A player can have multiple sets of bowling figures per innings if they have multiple identities in that innings,
            // which could happen if 'John Smith' was also entered as 'John', for example. These must be counted separately. 
            // Although a combined set of bowling figures might make sense for the same player, combined batting innings for that player
            // would not (there's no way to combine the dismissals) and figures within a match must be treated consistently.
            // 
            // This query groups sets of bowling figures by MatchTeamId, MatchInningsPair and PlayerIdentityId, which means one result per identity, per match innings.
            var bestFiguresSql = $@"FROM {Tables.PlayerInMatchStatistics} 
								WHERE Wickets IS NOT NULL {where}
								GROUP BY MatchTeamId, MatchInningsPair, PlayerIdentityId
								ORDER BY SUM(Wickets) DESC, CASE WHEN SUM(RunsConceded) IS NULL THEN 0 ELSE 1 END DESC, SUM(RunsConceded) ASC";


            var sql = $@"SELECT TotalInnings, TotalInningsWithRunsConceded, TotalInningsWithBallsBowled, TotalOvers, TotalMaidens, TotalRunsConceded, TotalWickets, 
                                FiveWicketInnings, BestInningsWickets, BestInningsRunsConceded, Economy, Average, StrikeRate
                         FROM (
	                        SELECT 
                                ({totalInningsSql}) AS TotalInnings,
                                ({totalInningsWithRunsSql}) AS TotalInningsWithRunsConceded,
                                ({totalInningsWithBallsBowledSql}) AS TotalInningsWithBallsBowled,
                                (SELECT SUM(BallsBowled)/{StatisticsConstants.BALLS_PER_OVER} + CAST((SUM(BallsBowled)%{StatisticsConstants.BALLS_PER_OVER})AS DECIMAL) / 10 FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where}) AS TotalOvers,
                                (SELECT SUM(Maidens) FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where}) AS TotalMaidens,
                                (SELECT SUM(RunsConceded) FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where}) AS TotalRunsConceded,
                                (SELECT SUM(Wickets) FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where}) AS TotalWickets,
                                (SELECT COUNT(MatchTeamId) FROM (SELECT MatchTeamId FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where} GROUP BY MatchTeamId, MatchInningsPair, PlayerIdentityId HAVING SUM(Wickets) >= 5) AS FiveWicketInnings) AS FiveWicketInnings,
                                (SELECT TOP 1 SUM(Wickets) {bestFiguresSql}) AS BestInningsWickets,
                                (SELECT TOP 1 SUM(RunsConceded) {bestFiguresSql}) AS BestInningsRunsConceded,
                                (SELECT CASE WHEN SUM(BallsBowled) > 0 THEN SUM(RunsConceded)/(CAST(SUM(BallsBowled) AS DECIMAL)/{StatisticsConstants.BALLS_PER_OVER}) ELSE NULL END FROM {Tables.PlayerInMatchStatistics} WHERE RunsConceded IS NOT NULL {where}) AS Economy,
                                (SELECT CASE WHEN SUM(Wickets) > 0 THEN CAST(SUM(RunsConceded) AS DECIMAL)/SUM(Wickets) ELSE NULL END FROM {Tables.PlayerInMatchStatistics} WHERE RunsConceded IS NOT NULL {where}) AS Average,
                                (SELECT CASE WHEN SUM(Wickets) > 0 THEN CAST(SUM(BallsBowled) AS DECIMAL)/SUM(Wickets) ELSE NULL END FROM {Tables.PlayerInMatchStatistics} WHERE BallsBowled IS NOT NULL {where}) AS StrikeRate
	                     ) AS BowlingStatistics";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<BowlingStatistics>(sql, parameters).ConfigureAwait(false);
            }
        }

        public async Task<FieldingStatistics> ReadFieldingStatistics(StatisticsFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (filter?.Player?.PlayerId == null) { throw new ArgumentException("Player.PlayerId must be specified", nameof(filter)); }

            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);

            var sql = $@"SELECT TotalCatches, MostCatches, TotalRunOuts, MostRunOuts
                         FROM (
	                        SELECT 
                                (SELECT SUM(Catches) FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where}) AS TotalCatches,
                                (SELECT SUM(RunOuts) FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where}) AS TotalRunOuts,
                                (SELECT MAX(Catches) FROM (SELECT SUM(Catches) AS Catches FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where} GROUP BY MatchTeamId, MatchInningsPair) AS CatchesPerInnings) AS MostCatches,
                                (SELECT MAX(RunOuts) FROM (SELECT SUM(RunOuts) AS RunOuts FROM {Tables.PlayerInMatchStatistics} WHERE 1=1 {where} GROUP BY MatchTeamId, MatchInningsPair) AS RunOutsPerInnings) AS MostRunOuts
	                     ) AS FieldingStatistics";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<FieldingStatistics>(sql, parameters).ConfigureAwait(false);
            }
        }
    }
}
