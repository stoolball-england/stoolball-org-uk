﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    public class SqlServerStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public SqlServerStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        public async Task<int> ReadTotalPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalBestFiguresInAMatch("RunsScored", 0, filter).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalBestFiguresInAMatch(string primaryFieldName, int minimumValue, StatisticsFilter filter)
        {
            var (where, parameters) = BuildQuery(filter);
            where = $"WHERE {primaryFieldName} IS NOT NULL AND {primaryFieldName} >= {minimumValue} {where}";
            return await ReadTotalResultsForPagedQuery(where, parameters).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalResultsForPagedQuery(string where, Dictionary<string, object> parameters, string groupBy = null, string having = null)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM (SELECT PlayerInMatchStatisticsId FROM {Tables.PlayerInMatchStatistics} {where} {groupBy} {having}) AS total", parameters).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<PlayerInningsResult>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder)
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

            return await ReadBestFiguresInAMatch("RunsScored", new[] { "PlayerInningsId", "DismissalType", "BallsFaced" }, orderByFields, 0, filter).ConfigureAwait(false);
        }


        /// <summary> 
        /// Gets best performances in a match based on the specified fields and filters
        /// </summary> 
        private async Task<IEnumerable<PlayerInningsResult>> ReadBestFiguresInAMatch(string primaryFieldName, IEnumerable<string> selectFields, IEnumerable<string> orderByFields, int minimumValue, StatisticsFilter filter)
        {
            var select = $@"SELECT PlayerId, PlayerRoute, PlayerIdentityId, PlayerIdentityName, TeamId, TeamName,
                OppositionTeamId AS TeamId, OppositionTeamName AS TeamName, MatchRoute, MatchStartTime AS StartTime, MatchName, 
                {primaryFieldName}, {string.Join(", ", selectFields)}";

            var (where, parameters) = BuildQuery(filter);
            where = $"WHERE {primaryFieldName} IS NOT NULL AND {primaryFieldName} >= {minimumValue} {where}";

            var orderBy = orderByFields.Any() ? "ORDER BY " + string.Join(", ", orderByFields) : string.Empty;

            var maxResults = string.Empty;
            if (filter.MaxResults.HasValue)
            {
                // Need to get the value at the last position to show, but first must check there
                // are at least as many records as the total requested, and set a lower limit if not
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var result = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} {where}", parameters).ConfigureAwait(false);
                    var lastPositionLimit = ((result > 0 && result < filter.MaxResults) ? result - 1 : (filter.MaxResults - 1)).ToString();
                    maxResults = $"AND {primaryFieldName} >= (SELECT {primaryFieldName} FROM {Tables.PlayerInMatchStatistics} {where} {orderBy} OFFSET {lastPositionLimit} ROWS FETCH NEXT 1 ROWS ONLY) ";
                }
            }

            var limit = LimitByPage(filter);
            var sql = $"{select} FROM {Tables.PlayerInMatchStatistics} {where} {maxResults} {orderBy} {limit}";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QueryAsync<Player, PlayerIdentity, Team, Team, MatchListing, PlayerInnings, PlayerInningsResult>(sql,
                    (player, playerIdentity, team, oppositionTeam, match, playerInnings) =>
                    {
                        player.PlayerIdentities.Add(playerIdentity);
                        return new PlayerInningsResult
                        {
                            Player = player,
                            Team = team,
                            OppositionTeam = oppositionTeam,
                            Match = match,
                            PlayerInnings = playerInnings
                        };
                    },
                    parameters,
                    splitOn: $"PlayerIdentityId, TeamId, TeamId, MatchRoute, {primaryFieldName}").ConfigureAwait(false);
            }
        }

        /// <summary> 
        /// Adds standard filters to the WHERE clause
        /// </summary> 
        private static (string where, Dictionary<string, object> parameters) BuildQuery(StatisticsFilter filter)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (filter.PlayerOfTheMatch.HasValue)
            {
                where.Add("PlayerOfTheMatch = 1");
            }

            if (filter.PlayerIds.Any())
            {
                where.Add("PlayerId IN @PlayerIds");
                parameters.Add("@PlayerIds", filter.PlayerIds);
            }

            if (filter.BowledByPlayerIdentityIds.Any())
            {
                where.Add("BowledByPlayerIdentityId IN @BowlerPlayerIdentityIds");
                parameters.Add("@BowlerPlayerIdentityIds", filter.BowledByPlayerIdentityIds);
            }

            if (filter.CaughtByPlayerIdentityIds.Any())
            {
                where.Add("CaughtByPlayerIdentityId IN @CaughtByPlayerIdentityIds");
                parameters.Add("@CaughtByPlayerIdentityIds", filter.CaughtByPlayerIdentityIds);
            }

            if (filter.RunOutByPlayerIdentityIds.Any())
            {
                where.Add("RunOutByPlayerIdentityId IN @RunOutByPlayerIdentityIds");
                parameters.Add("@RunOutByPlayerIdentityIds", filter.RunOutByPlayerIdentityIds);
            }

            if (filter.SwapTeamAndOppositionFilters)
            {
                // When querying by the player id of a fielder, flip team && opposition because
                // the fielder's team is the opposition_id
                if (filter.TeamIds.Any())
                {
                    where.Add("OppositionTeamId IN @OppositionTeamIds");
                    parameters.Add("@OppositionTeamIds", filter.TeamIds);
                }

                if (filter.OppositionTeamIds.Any())
                {
                    where.Add("TeamId IN @TeamIds");
                    parameters.Add("@TeamIds", filter.OppositionTeamIds);
                }

                if (filter.WonMatch.HasValue)
                {
                    where.Add("WonMatch = @WonMatch");
                    parameters.Add("@WonMatch", filter.WonMatch.Value ? 0 : 1);
                }
            }
            else
            {
                if (filter.TeamIds.Any())
                {
                    where.Add("TeamId IN @TeamIds");
                    parameters.Add("@TeamIds", filter.TeamIds);
                }

                if (filter.OppositionTeamIds.Any())
                {
                    where.Add("OppositionTeamId IN @OppositionTeamIds");
                    parameters.Add("@OppositionTeamIds", filter.OppositionTeamIds);
                }

                if (filter.WonMatch.HasValue)
                {
                    where.Add("WonMatch = @WonMatch");
                    parameters.Add("@WonMatch", filter.WonMatch.Value ? 1 : 0);
                }
            }

            if (filter.SeasonIds.Any())
            {
                where.Add("SeasonId IN @SeasonIds");
                parameters.Add("@SeasonIds", filter.SeasonIds);
            }

            if (filter.CompetitionIds.Any())
            {
                where.Add("CompetitionId IN @CompetitionIds");
                parameters.Add("@CompetitionIds", filter.CompetitionIds);
            }

            if (filter.MatchLocationIds.Any())
            {
                where.Add("MatchLocationId IN @MatchLocationIds");
                parameters.Add("@MatchLocationIds", filter.MatchLocationIds);
            }

            if (filter.MatchTypes.Any())
            {
                where.Add("MatchType IN @MatchTypes");
                parameters.Add("@MatchTypes", filter.MatchTypes.Select(x => x.ToString()));
            }

            if (filter.PlayerTypes.Any())
            {
                where.Add("MatchPlayerType IN @MatchPlayerTypes");
                parameters.Add("@MatchPlayerTypes", filter.PlayerTypes.Select(x => x.ToString()));
            }

            if (filter.BattingPositions.Any())
            {
                where.Add("BattingPosition IN @BattingPositions");
                parameters.Add("@BattingPositions", filter.BattingPositions);
            }

            if (filter.TournamentIds.Any())
            {
                where.Add("TournamentId IN @TournamentIds");
                parameters.Add("@TournamentIds", filter.TournamentIds);
            }

            if (filter.DismissalTypes.Any())
            {
                where.Add("DismissalType IN @DismissalTypes");
                parameters.Add("@DismissalTypes", filter.DismissalTypes.Select(x => x.ToString()));
            }

            if (filter.FromDate.HasValue)
            {
                where.Add("MatchStartTime >= @FromDate");
                parameters.Add("@FromDate", filter.FromDate);
            }

            if (filter.UntilDate.HasValue)
            {
                where.Add("MatchStartTime <= @UntilDate");
                parameters.Add("@UntilDate", filter.UntilDate);
            }

            if (filter.BattingFirst.HasValue)
            {
                if (filter.SwapBattingFirstFilter)
                {
                    where.Add("BattedFirst = @BattedFirst");
                    parameters.Add("@BattedFirst", filter.BattingFirst.Value ? 0 : 1);
                }
                else
                {
                    where.Add("BattedFirst = @BattedFirst");
                    parameters.Add("@BattedFirst", filter.BattingFirst.Value ? 1 : 0);
                }
            }

            return (where.Count > 0 ? " AND " + string.Join(" AND ", where) : string.Empty, parameters);
        }

        /// <summary> 
        /// Generates a LIMIT clause if appropriate
        /// </summary> 
        private static string LimitByPage(StatisticsFilter filter)
        {
            // Limit main query to just the current page
            return $"OFFSET {(filter.PageSize * (filter.PageNumber - 1))} ROWS FETCH NEXT {filter.PageSize} ROWS ONLY";
        }
    }
}