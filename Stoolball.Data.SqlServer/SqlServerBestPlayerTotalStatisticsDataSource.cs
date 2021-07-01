using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    public class SqlServerBestPlayerTotalStatisticsDataSource : IBestPlayerTotalStatisticsDataSource, ICacheableBestPlayerTotalStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IStatisticsQueryBuilder _statisticsQueryBuilder;
        private readonly IPlayerDataSource _playerDataSource;

        public SqlServerBestPlayerTotalStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IStatisticsQueryBuilder statisticsQueryBuilder, IPlayerDataSource playerDataSource)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _statisticsQueryBuilder = statisticsQueryBuilder ?? throw new ArgumentNullException(nameof(statisticsQueryBuilder));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            var outerQuery = @"SELECT PlayerId, PlayerRoute, TotalMatches, TotalInnings, Total, 
                                CASE WHEN TotalDismissals > 0 THEN CAST(Total AS DECIMAL)/ TotalDismissals ELSE NULL END AS Average
                         FROM(
                            <<QUERY>>
                         ) AS BestTotal
                         ORDER BY Total DESC, TotalInnings ASC, TotalMatches ASC";

            var extraSelectFields = $", (SELECT COUNT(PlayerInMatchStatisticsId) FROM { Tables.PlayerInMatchStatistics } WHERE PlayerId = s.PlayerId AND PlayerWasDismissed = 1 <<WHERE>>) AS TotalDismissals";

            return await ReadBestPlayerTotal("RunsScored", false, extraSelectFields, outerQuery, $"AND (DismissalType IS NULL OR DismissalType != { (int)DismissalType.DidNotBat})", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostWickets(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadBestPlayerTotal("Wickets", true, null, null, "AND BowlingFiguresId IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostCatches(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadBestPlayerTotal("Catches", true, null, null, string.Empty, filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunOuts(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadBestPlayerTotal("RunOuts", true, null, null, string.Empty, filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter)
        {
            return await ReadTotalPlayersWithData("RunsScored", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithWickets(StatisticsFilter filter)
        {
            return await ReadTotalPlayersWithData("Wickets", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithCatches(StatisticsFilter filter)
        {
            return await ReadTotalPlayersWithData("Catches", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithRunOuts(StatisticsFilter filter)
        {
            return await ReadTotalPlayersWithData("RunOuts", filter).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalPlayersWithData(string fieldName, StatisticsFilter filter)
        {
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(DISTINCT PlayerId) FROM {Tables.PlayerInMatchStatistics} WHERE {fieldName} > 0 {where}", parameters).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestPlayerTotal(string fieldName, bool isFieldingStatistic, string extraSelectFields, string outerQueryIncludingOrderBy, string totalInningsFilter, StatisticsFilter filter)
        {
            var clonedFilter = filter.Clone();
            clonedFilter.SwapBattingFirstFilter = isFieldingStatistic;
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(clonedFilter);

            var group = "GROUP BY PlayerId, PlayerRoute";
            var having = $"HAVING SUM({fieldName}) > 0";

            // The result set can be limited in two mutually-exlusive ways:
            // 1. Max results (eg top ten) but where results beyond but equal to the max are also included
            // 2. Paging
            var preQuery = string.Empty;
            var offsetWithExtraResults = string.Empty;
            var offsetPaging = string.Empty;
            if (clonedFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual.HasValue)
            {
                // Get the values from what should be the last row according to the maximum number of results.
                preQuery = $@"DECLARE @MaxResult int;
                            SELECT @MaxResult = SUM({fieldName}) FROM {Tables.PlayerInMatchStatistics} WHERE {fieldName} IS NOT NULL AND {fieldName} >= 0 {where} {group} {having} ORDER BY SUM({fieldName}) DESC
                            OFFSET {clonedFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual - 1} ROWS FETCH NEXT 1 ROWS ONLY; ";

                // If @MaxResult IS NULL there are fewer rows than the requested maximum, so just fetch all.
                // Otherwise look for results that are greater than or equal to the value(s) in the last row retrieved above.
                offsetWithExtraResults = $"AND (@MaxResult IS NULL OR SUM({fieldName}) >= @MaxResult) ";

                // Add an ORDER BY clause to sort the results, unless we're relying on an outer query to do that because it's not valid in a sub-query
                if (string.IsNullOrEmpty(outerQueryIncludingOrderBy))
                {
                    offsetWithExtraResults += $"ORDER BY SUM({fieldName}) DESC, TotalInnings ASC, TotalMatches ASC";
                }
            }
            else
            {
                offsetPaging = $"ORDER BY SUM({fieldName}) DESC, TotalInnings ASC, TotalMatches ASC OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("@PageOffset", clonedFilter.Paging.PageSize * (clonedFilter.Paging.PageNumber - 1));
                parameters.Add("@PageSize", clonedFilter.Paging.PageSize);
            }

            var totalInningsQuery = !string.IsNullOrEmpty(totalInningsFilter) ? $"SELECT COUNT(PlayerInMatchStatisticsId) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {totalInningsFilter} {where}" : "NULL";

            var sql = $@"SELECT PlayerId, PlayerRoute,
		                                (SELECT COUNT(DISTINCT MatchId) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {where}) AS TotalMatches,
		                                ({totalInningsQuery}) AS TotalInnings,
		                                (SELECT SUM({ fieldName}) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {where}) AS Total
                                        <<SELECT>>
                                 FROM {Tables.PlayerInMatchStatistics} AS s 
                                 WHERE {fieldName} IS NOT NULL AND {fieldName} >= 0 {where} 
                                 {group} 
                                 {having} 
                                 {offsetWithExtraResults} 
                                 {offsetPaging}";

            if (!string.IsNullOrEmpty(extraSelectFields))
            {
                extraSelectFields = extraSelectFields.Replace("<<WHERE>>", where);
                sql = sql.Replace("<<SELECT>>", extraSelectFields);
            }
            else
            {
                sql = sql.Replace("<<SELECT>>", string.Empty);
            }

            if (!string.IsNullOrEmpty(outerQueryIncludingOrderBy))
            {
                sql = outerQueryIncludingOrderBy.Replace("<<QUERY>>", sql);
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<Player, BestStatistic, StatisticsResult<BestStatistic>>($"{preQuery} {sql}",
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