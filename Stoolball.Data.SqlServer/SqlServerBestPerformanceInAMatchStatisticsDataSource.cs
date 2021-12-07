using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    public class SqlServerBestPerformanceInAMatchStatisticsDataSource : IBestPerformanceInAMatchStatisticsDataSource, ICacheableBestPerformanceInAMatchStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IStatisticsQueryBuilder _statisticsQueryBuilder;

        public SqlServerBestPerformanceInAMatchStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IStatisticsQueryBuilder statisticsQueryBuilder)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _statisticsQueryBuilder = statisticsQueryBuilder ?? throw new ArgumentNullException(nameof(statisticsQueryBuilder));
        }

        public async virtual Task<int> ReadTotalPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalBestFiguresInAMatch("RunsScored", null, filter).ConfigureAwait(false);
        }

        public async virtual Task<int> ReadTotalBowlingFigures(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalBestFiguresInAMatch("Wickets", 0, filter).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalBestFiguresInAMatch(string primaryFieldName, int? minimumValue, StatisticsFilter filter)
        {
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            where = $"WHERE {primaryFieldName} IS NOT NULL {where}";
            if (minimumValue != null)
            {
                where += $" AND {primaryFieldName} >= {minimumValue}";
            }
            return await ReadTotalResultsForPagedQuery(where, parameters).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalResultsForPagedQuery(string where, Dictionary<string, object> parameters, string groupBy = null, string having = null)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM (SELECT PlayerInMatchStatisticsId FROM {Tables.PlayerInMatchStatistics} {where} {groupBy} {having}) AS total", parameters).ConfigureAwait(false);
            }
        }

        public async virtual Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();

            var orderByFields = new List<string>();
            if (sortOrder == StatisticsSortOrder.LatestFirst)
            {
                orderByFields.Add("MatchStartTime DESC");
            }
            else if (sortOrder == StatisticsSortOrder.BestFirst)
            {
                orderByFields.Add("RunsScored DESC");
                orderByFields.Add("DismissalType ASC");
            }
            else
            {
                throw new InvalidOperationException();
            }

            return await ReadBestFiguresInAMatch<PlayerInnings>("RunsScored", "PlayerWasDismissed", new[] { "PlayerInningsId", "DismissalType", "BallsFaced" }, orderByFields, null, filter).ConfigureAwait(false);
        }

        public async virtual Task<IEnumerable<StatisticsResult<BowlingFigures>>> ReadBowlingFigures(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();

            var orderByFields = new List<string>();
            if (sortOrder == StatisticsSortOrder.LatestFirst)
            {
                orderByFields.Add("MatchStartTime DESC");
            }
            else if (sortOrder == StatisticsSortOrder.BestFirst)
            {
                orderByFields.Add("Wickets DESC");
                orderByFields.Add("HasRunsConceded DESC");
                orderByFields.Add("RunsConceded ASC");
            }
            else
            {
                throw new InvalidOperationException();
            }

            // NOTE: Don't check for RunsConceded IS NOT NULL in this stat, because 5/NULL is still better than 4/20.
            // Wickets, even if 0, means the player bowled in the match, so check it is not null.
            // Can trust wickets, not other fields, because wickets figure is generated whether bowling is recorded on batting and/or bowling card.
            return await ReadBestFiguresInAMatch<BowlingFigures>("Wickets", "RunsConceded", new[] { "BowlingFiguresId", "RunsConceded", "Overs", "Maidens" }, orderByFields, 0, filter).ConfigureAwait(false);
        }


        /// <summary> 
        /// Gets best performances in a match based on the specified fields and filters
        /// </summary> 
        private async Task<IEnumerable<StatisticsResult<T>>> ReadBestFiguresInAMatch<T>(string primaryFieldName, string secondaryFieldNameForMaxResults, IEnumerable<string> selectFields, IEnumerable<string> orderByFields, int? minimumValue, StatisticsFilter filter)
        {
            var select = $@"SELECT PlayerId, PlayerRoute, PlayerIdentityId, PlayerIdentityName, TeamId, TeamName,
                OppositionTeamId AS TeamId, OppositionTeamName AS TeamName, MatchRoute, MatchStartTime AS StartTime, MatchName, 
                {primaryFieldName}, {string.Join(", ", selectFields)}";

            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            where = $"WHERE {primaryFieldName} IS NOT NULL {where}";
            if (minimumValue != null)
            {
                where += $" AND { primaryFieldName} >= { minimumValue}";
            }

            var orderBy = orderByFields.Any() ? "ORDER BY " + string.Join(", ", orderByFields) : string.Empty;

            // The result set can be limited in two mutually-exlusive ways:
            // 1. Max results (eg top ten) but where results beyond but equal to the max are also included
            // 2. Paging
            var preQuery = string.Empty;
            var offsetWithExtraResults = string.Empty;
            var offsetPaging = string.Empty;
            if (filter.MaxResultsAllowingExtraResultsIfValuesAreEqual.HasValue)
            {
                // Get the values from what should be the last row according to the maximum number of results.
                preQuery = $@"DECLARE @MaxResult1 int, @MaxResult2 int;
                            SELECT @MaxResult1 = {primaryFieldName}, @MaxResult2 = {secondaryFieldNameForMaxResults} FROM {Tables.PlayerInMatchStatistics} {where} {orderBy}
                            OFFSET {filter.MaxResultsAllowingExtraResultsIfValuesAreEqual - 1} ROWS FETCH NEXT 1 ROWS ONLY; ";

                // If @MaxResult1 IS NULL there are fewer rows than the requested maximum, so just fetch all.
                // Otherwise look for results that are greater than or equal to the value(s) in the last row retrieved above.
                offsetWithExtraResults = $"AND (@MaxResult1 IS NULL OR ({primaryFieldName} > @MaxResult1 OR ({primaryFieldName} = @MaxResult1 AND ({secondaryFieldNameForMaxResults} <= @MaxResult2 OR @MaxResult2 IS NULL)))) ";
            }
            else
            {
                offsetPaging = $"OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("@PageOffset", filter.Paging.PageSize * (filter.Paging.PageNumber - 1));
                parameters.Add("@PageSize", filter.Paging.PageSize);
            }

            var sql = $"{preQuery} {select} FROM {Tables.PlayerInMatchStatistics} {where} {offsetWithExtraResults} {orderBy} {offsetPaging}";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QueryAsync<Player, PlayerIdentity, Team, Team, MatchListing, T, StatisticsResult<T>>(sql,
                    (player, playerIdentity, team, oppositionTeam, match, playerInnings) =>
                    {
                        player.PlayerIdentities.Add(playerIdentity);
                        return new StatisticsResult<T>
                        {
                            Player = player,
                            Team = team,
                            OppositionTeam = oppositionTeam,
                            Match = match,
                            Result = playerInnings
                        };
                    },
                    parameters,
                    splitOn: $"PlayerIdentityId, TeamId, TeamId, MatchRoute, {primaryFieldName}",
                    commandTimeout: 60).ConfigureAwait(false);
            }
        }
    }
}