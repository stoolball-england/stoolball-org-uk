﻿using System;
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

        public async Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter statisticsFilter)
        {
            if (statisticsFilter == null) { throw new ArgumentNullException(nameof(statisticsFilter)); }
            if (statisticsFilter?.Player?.PlayerId == null) { throw new ArgumentException("Player.PlayerId must be specified", nameof(statisticsFilter)); }

            var sql = $@"SELECT TotalInnings, TotalInningsWithRunsScored, TotalInningsWithRunsScoredAndBallsFaced, NotOuts, TotalRunsScored, Fifties, Hundreds, BestInningsRunsScored, BestInningsWasDismissed, StrikeRate,
                         CASE WHEN DismissalsWithRunsScored > 0 THEN CAST(TotalRunsScored AS DECIMAL)/DismissalsWithRunsScored ELSE NULL END AS Average
                         FROM (
	                        SELECT 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType != {(int)DismissalType.DidNotBat}) AS TotalInnings,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL) AS TotalInningsWithRunsScored, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType != {(int)DismissalType.DidNotBat} AND RunsScored IS NOT NULL AND BallsFaced IS NOT NULL) AS TotalInningsWithRunsScoredAndBallsFaced, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType IN ({(int)DismissalType.NotOut},{(int)DismissalType.Retired},{(int)DismissalType.RetiredHurt})) AS NotOuts, 
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId AND DismissalType > {(int)DismissalType.RetiredHurt} AND RunsScored IS NOT NULL) AS DismissalsWithRunsScored, 
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
                    statisticsFilter.Player.PlayerId
                })
                .ConfigureAwait(false);
            }
        }
    }
}