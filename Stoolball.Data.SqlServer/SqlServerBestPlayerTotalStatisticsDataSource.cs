using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    public class SqlServerBestPlayerTotalStatisticsDataSource : IBestPlayerTotalStatisticsDataSource, ICacheableBestTotalStatisticsDataSource
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

        public async Task<IEnumerable<StatisticsResult<BestTotal>>> ReadMostRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadBestPlayerTotal("RunsScored", false, filter).ConfigureAwait(false);
        }

        public async Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter)
        {
            return await ReadTotalPlayersWithData("RunsScored", filter).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalPlayersWithData(string fieldName, StatisticsFilter filter)
        {
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(DISTINCT PlayerId) FROM {Tables.PlayerInMatchStatistics} WHERE {fieldName} > 0 {where}", parameters).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<StatisticsResult<BestTotal>>> ReadBestPlayerTotal(string fieldName, bool isFieldingStatistic, StatisticsFilter filter)
        {
            var select = $@"SELECT PlayerId, PlayerRoute,
		                        (SELECT COUNT(DISTINCT MatchId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId) AS TotalMatches,
		                        (SELECT COUNT(PlayerInMatchStatisticsId) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId AND DismissalType != {(int)DismissalType.DidNotBat}) AS TotalInnings,
		                        (SELECT SUM({fieldName}) FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = s.PlayerId) AS Total";

            var clonedFilter = filter.Clone();
            clonedFilter.SwapBattingFirstFilter = isFieldingStatistic;
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(clonedFilter);
            where = $"WHERE {fieldName} IS NOT NULL AND {fieldName} >= 0 {where}";

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
                            SELECT @MaxResult = SUM({fieldName}) FROM {Tables.PlayerInMatchStatistics} {where} {group} {having} ORDER BY SUM({fieldName}) DESC
                            OFFSET {clonedFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual - 1} ROWS FETCH NEXT 1 ROWS ONLY; ";

                // If @MaxResult IS NULL there are fewer rows than the requested maximum, so just fetch all.
                // Otherwise look for results that are greater than or equal to the value(s) in the last row retrieved above.
                offsetWithExtraResults = $"AND (@MaxResult IS NULL OR SUM({fieldName}) >= @MaxResult) ";
            }
            else
            {
                offsetPaging = $"OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("@PageOffset", clonedFilter.Paging.PageSize * (clonedFilter.Paging.PageNumber - 1));
                parameters.Add("@PageSize", clonedFilter.Paging.PageSize);
            }

            var sql = $"{preQuery} {select} FROM {Tables.PlayerInMatchStatistics} AS s {where} {group} {having} {offsetWithExtraResults} ORDER BY SUM({fieldName}) DESC, TotalInnings ASC, TotalMatches ASC {offsetPaging}";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<Player, BestTotal, StatisticsResult<BestTotal>>(sql,
                    (player, totals) =>
                    {
                        totals.Player = player;
                        return new StatisticsResult<BestTotal>
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