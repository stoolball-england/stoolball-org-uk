﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    public class SqlServerBestPlayerAverageStatisticsDataSource : IBestPlayerAverageStatisticsDataSource, ICacheableBestPlayerAverageStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IStatisticsQueryBuilder _statisticsQueryBuilder;
        private readonly IPlayerDataSource _playerDataSource;

        public SqlServerBestPlayerAverageStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IStatisticsQueryBuilder statisticsQueryBuilder, IPlayerDataSource playerDataSource)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _statisticsQueryBuilder = statisticsQueryBuilder ?? throw new ArgumentNullException(nameof(statisticsQueryBuilder));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadBestPlayerTotal("RunsScored", "CAST(PlayerWasDismissed AS INT)", "DESC", false, $"AND RunsScored IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBowlingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadBestPlayerTotal("RunsConceded", "Wickets", "ASC", true, $"AND RunsConceded IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestEconomyRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadBestPlayerTotal("RunsConceded", $"CAST(BallsBowled AS DECIMAL)/{StatisticsConstants.BALLS_PER_OVER}", "ASC", true, $"AND RunsConceded IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithBattingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalPlayersWithData("RunsScored", "CAST(PlayerWasDismissed AS INT)", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithBowlingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalPlayersWithData("RunsConceded", "Wickets", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithEconomyRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalPlayersWithData("RunsConceded", $"CAST(BallsBowled AS DECIMAL)/{StatisticsConstants.BALLS_PER_OVER}", filter).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalPlayersWithData(string divideThisField, string byThisField, StatisticsFilter filter)
        {
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = $@"SELECT COUNT(PlayerId) FROM (
                            SELECT PlayerId FROM {Tables.PlayerInMatchStatistics} 
                            WHERE {divideThisField} IS NOT NULL {where} 
                            GROUP BY PlayerId 
                            HAVING SUM({byThisField}) > 0 AND 
                                   COUNT(PlayerInMatchStatisticsId) >= @MinimumQualifyingInnings
                        ) AS Total";
                parameters.Add("@MinimumQualifyingInnings", filter.MinimumQualifyingInnings ?? 1);
                return await connection.ExecuteScalarAsync<int>(sql, parameters).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestPlayerTotal(string divideThisField, string byThisField, string sortOrder, bool isFieldingStatistic, string inningsFilter, StatisticsFilter filter)
        {
            var clonedFilter = filter.Clone();
            clonedFilter.SwapBattingFirstFilter = isFieldingStatistic;
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(clonedFilter);

            var sql = $@"SELECT PlayerId, PlayerRoute, TotalMatches, TotalInnings, Average
                         FROM(
                            SELECT PlayerId, PlayerRoute, CAST(SUM({divideThisField}) AS DECIMAL) / SUM({byThisField}) AS Average,
		                                (SELECT COUNT(DISTINCT MatchId) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {where}) AS TotalMatches,
		                                (SELECT COUNT(PlayerInMatchStatisticsId) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {inningsFilter} {where}) AS TotalInnings
                                 FROM {Tables.PlayerInMatchStatistics} AS s 
                                 WHERE 1=1 {inningsFilter} {where} 
                                 GROUP BY PlayerId, PlayerRoute
                                 HAVING SUM({byThisField}) > 0 
                                    AND (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {inningsFilter} {where}) >= @MinimumQualifyingInnings
                                ORDER BY CAST(SUM({divideThisField}) AS DECIMAL) / SUM({byThisField}) {sortOrder}, TotalInnings DESC, TotalMatches DESC 
                                OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY
                         ) AS BestAverage
                         ORDER BY Average {sortOrder}, TotalInnings DESC, TotalMatches DESC";

            parameters.Add("@MinimumQualifyingInnings", filter.MinimumQualifyingInnings ?? 1);
            parameters.Add("@PageOffset", clonedFilter.Paging.PageSize * (clonedFilter.Paging.PageNumber - 1));
            parameters.Add("@PageSize", clonedFilter.Paging.PageSize);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<Player, BestStatistic, StatisticsResult<BestStatistic>>(sql,
                    (player, totals) =>
                    {
                        totals.Player = player;
                        return new StatisticsResult<BestStatistic>
                        {
                            Result = totals
                        };
                    },
                    parameters,
                    splitOn: $"TotalMatches",
                    commandTimeout: 60).ConfigureAwait(false);

                var players = await _playerDataSource.ReadPlayers(new PlayerFilter { PlayerIds = results.Select(x => x.Result.Player.PlayerId.Value).ToList() }).ConfigureAwait(false);
                foreach (var result in results)
                {
                    result.Result.Player = players.Single(x => x.PlayerId == result.Result.Player.PlayerId);
                }

                return results;
            }
        }
    }
}