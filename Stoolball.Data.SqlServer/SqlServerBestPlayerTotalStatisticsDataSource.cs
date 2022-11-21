using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.Abstractions;
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
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostPlayerInnings(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadBestPlayerCount("RunsScored", " AND RunsScored IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunsScored(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();

            var outerQuery = @"SELECT PlayerId, TotalMatches, TotalInnings, Total, 
                                CASE WHEN TotalDismissals > 0 THEN CAST(Total AS DECIMAL)/ TotalDismissals ELSE NULL END AS Average
                         FROM(
                            <<QUERY>>
                         ) AS BestTotal
                         ORDER BY Total DESC, TotalInnings ASC, TotalMatches ASC";

            var extraSelectFields = $", (SELECT COUNT(PlayerInMatchStatisticsId) FROM { Tables.PlayerInMatchStatistics } WHERE PlayerId = s.PlayerId AND PlayerWasDismissed = 1 AND RunsScored IS NOT NULL <<WHERE>>) AS TotalDismissals";

            return await ReadBestPlayerSum("RunsScored", true, false, extraSelectFields, outerQuery, $"AND RunsScored IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostInningsWithBowling(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadBestPlayerCount("Wickets", " AND Wickets IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostWickets(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadBestPlayerSum("Wickets", false, true, null, null, "AND BowlingFiguresId IS NOT NULL", filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostCatches(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadBestPlayerSum("Catches", false, true, null, null, string.Empty, filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunOuts(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadBestPlayerSum("RunOuts", false, true, null, null, string.Empty, filter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostPlayerOfTheMatchAwards(StatisticsFilter? filter)
        {
            var clonedFilter = filter?.Clone() ?? new StatisticsFilter();

            // Remove this criterion because it's hard-coded in the query and we don't want it added to the general WHERE clause that also applies to the selection of TotalMatches 
            clonedFilter.PlayerOfTheMatch = null;

            // This needs a different query to other totals because the PlayerOfTheMatch=1 metric can be a duplicate if the player has more than one
            // row for the match, eg 2 innings match, batted twice in the same innings, or for both teams
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(clonedFilter);

            var sql = @$"SELECT PlayerId, 
		                        (SELECT COUNT(DISTINCT MatchId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {where}) AS TotalMatches,
		                        (SELECT COUNT(DISTINCT MatchId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId AND PlayerOfTheMatch = 1 {where}) AS Total
                                 FROM {Tables.PlayerInMatchStatistics} AS s
                                 WHERE CAST(PlayerOfTheMatch AS INT) > 0 {where}
                                 GROUP BY PlayerId
                                 ORDER BY COUNT(DISTINCT(MatchId)) DESC, TotalMatches ASC 
                                 OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@PageOffset", clonedFilter.Paging.PageSize * (clonedFilter.Paging.PageNumber - 1));
            parameters.Add("@PageSize", clonedFilter.Paging.PageSize);

            return await ReadResults(sql, parameters, clonedFilter).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadTotalPlayersWithData("RunsScored", filter, true).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithWickets(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadTotalPlayersWithData("Wickets", filter, false).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithCatches(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadTotalPlayersWithData("Catches", filter, false).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithRunOuts(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadTotalPlayersWithData("RunOuts", filter, false).ConfigureAwait(false);
        }

        ///  <inheritdoc/>
        public async Task<int> ReadTotalPlayersWithPlayerOfTheMatchAwards(StatisticsFilter? filter)
        {
            filter = filter ?? new StatisticsFilter();
            return await ReadTotalPlayersWithData("PlayerOfTheMatch", filter, false).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalPlayersWithData(string fieldName, StatisticsFilter filter, bool fieldValueCanBeNegative)
        {
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var minimumRequirement = fieldValueCanBeNegative ? $"{fieldName} IS NOT NULL" : $"{fieldName} > 0";
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(DISTINCT PlayerId) FROM {Tables.PlayerInMatchStatistics} WHERE {minimumRequirement} {where}", parameters).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestPlayerSum(string fieldName, bool fieldValueCanBeNegative, bool isFieldingStatistic, string? extraSelectFields, string? outerQueryIncludingOrderBy, string totalInningsFilter, StatisticsFilter filter)
        {
            var clonedFilter = filter.Clone();
            clonedFilter.SwapBattingFirstFilter = isFieldingStatistic;
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(clonedFilter);

            var group = "GROUP BY PlayerId";
            var having = fieldValueCanBeNegative ? "HAVING 1=1" : $"HAVING SUM({fieldName}) > 0";
            var minimumValue = fieldValueCanBeNegative ? string.Empty : $"AND {fieldName} >= 0";

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
                            SELECT @MaxResult = SUM({fieldName}) FROM {Tables.PlayerInMatchStatistics} WHERE {fieldName} IS NOT NULL {minimumValue} {where} {group} {having} ORDER BY SUM({fieldName}) DESC
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

            var sql = $@"SELECT PlayerId, 
		                    (SELECT COUNT(DISTINCT MatchId) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {where}) AS TotalMatches,
		                    ({totalInningsQuery}) AS TotalInnings,
		                    (SELECT SUM({ fieldName}) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {where}) AS Total
                            <<SELECT>>
                        FROM {Tables.PlayerInMatchStatistics} AS s 
                        WHERE {fieldName} IS NOT NULL {minimumValue} {where} 
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

            return await ReadResults($"{preQuery} {sql}", parameters, filter).ConfigureAwait(false);
        }

        private async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestPlayerCount(string fieldName, string totalInningsFilter, StatisticsFilter filter)
        {
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);

            // For context about the number of qualifying matches, exclude any filter which would remove results even though the player featured in a qualifying match
            var filterWithoutMinimumPerformance = filter.Clone();
            filterWithoutMinimumPerformance.MinimumRunsScored = null;
            filterWithoutMinimumPerformance.MinimumWicketsTaken = null;
            var (whereWithoutMinimumPerformance, _) = _statisticsQueryBuilder.BuildWhereClause(filterWithoutMinimumPerformance);

            var sql = @$"SELECT PlayerId, 
                            (SELECT COUNT(DISTINCT MatchId) FROM { Tables.PlayerInMatchStatistics } WHERE PlayerId = s.PlayerId {whereWithoutMinimumPerformance}) AS TotalMatches,
                            (SELECT COUNT(PlayerInMatchStatisticsId) FROM { Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId {totalInningsFilter} {whereWithoutMinimumPerformance}) AS TotalInnings,
                            COUNT({fieldName}) AS Total
                         FROM {Tables.PlayerInMatchStatistics} s
                         WHERE {fieldName} IS NOT NULL {where}
                         GROUP BY PlayerId
                         ORDER BY COUNT({fieldName}) DESC
                         OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@PageOffset", filter.Paging.PageSize * (filter.Paging.PageNumber - 1));
            parameters.Add("@PageSize", filter.Paging.PageSize);

            return await ReadResults(sql, parameters, filter).ConfigureAwait(false);
        }

        private async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadResults(string sql, Dictionary<string, object>? parameters, StatisticsFilter filter)
        {
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

                var playerFilter = filter.ToPlayerFilter();
                playerFilter.PlayerIds = results.Select(x => x.Result.Player.PlayerId!.Value).ToList();
                var players = await _playerDataSource.ReadPlayers(playerFilter).ConfigureAwait(false);
                foreach (var result in results)
                {
                    result.Result.Player = players.Single(x => x.PlayerId == result.Result.Player.PlayerId);
                }

                return results;
            }
        }

        public Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostBowlingFigures(StatisticsFilter filter)
        {
            throw new NotImplementedException();
        }
    }
}