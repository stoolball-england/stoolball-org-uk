using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.Abstractions;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball player data from the Umbraco database
    /// </summary>
    public class SqlServerPlayerDataSource : IPlayerDataSource, ICacheablePlayerDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;
        private readonly IStatisticsQueryBuilder _statisticsQueryBuilder;

        public SqlServerPlayerDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser, IStatisticsQueryBuilder statisticsQueryBuilder)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
            _statisticsQueryBuilder = statisticsQueryBuilder ?? throw new ArgumentNullException(nameof(statisticsQueryBuilder));
        }

        /// <inheritdoc/>
        public async Task<List<Player>> ReadPlayers(PlayerFilter? filter)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await ReadPlayers(filter, connection).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public async Task<List<Player>> ReadPlayers(PlayerFilter? filter, IDbConnection connection)
        {
            if (filter is null)
            {
                filter = new PlayerFilter();
            }

            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var sql = $@"SELECT DISTINCT PlayerId, PlayerRoute, 
                                PlayerIdentityId, PlayerIdentityName, MIN(MatchStartTime) AS FirstPlayed,  MAX(MatchStartTime) AS LastPlayed, COUNT(DISTINCT MatchId) AS TotalMatches,
                                TeamId, TeamName 
                                FROM {Tables.PlayerInMatchStatistics} AS stats 
                                <<WHERE>>
                                GROUP BY stats.PlayerId, stats.PlayerRoute, stats.PlayerIdentityId, stats.PlayerIdentityName, stats.TeamId, stats.TeamName";

            var (where, parameters) = BuildWhereClause(filter);
            sql = sql.Replace("<<WHERE>>", $"WHERE 1=1 {where}");

            var rawResults = (await connection.QueryAsync<Player, PlayerIdentity, Team, Player>(sql,
                (player, identity, team) =>
                {
                    identity.Team = team;
                    identity.Player = player;
                    player.PlayerIdentities.Add(identity);
                    return player;
                },
                new DynamicParameters(parameters),
                splitOn: "PlayerIdentityId, TeamId").ConfigureAwait(false)).ToList();

            return rawResults.GroupBy(x => x.PlayerId).Select(group =>
            {
                var player = group.First();
                player.PlayerIdentities = group.Select(x => x.PlayerIdentities.Single()).OfType<PlayerIdentity>().ToList();
                return player;
            }).ToList();
        }

        /// <summary>
        /// Gets a list of player identities based on a query
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no player identities are found.</returns>
        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerFilter? filter)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                // Get PlayerIdentity data from the original tables rather than PlayerInMatchStatistics because the original tables
                // will be updated when a player identity is renamed, and we need to see the change immediately.
                // Updates to PlayerInMatchStatistics are done asynchronously and the data will not be updated by the time this is called again.

                const string PROBABILITY_CALCULATION = "COUNT(DISTINCT MatchId)*10-DATEDIFF(DAY,MAX(MatchStartTime),GETDATE())";
                var sql = $@"SELECT stats.PlayerIdentityId, pi.PlayerIdentityName, pi.RouteSegment, {PROBABILITY_CALCULATION} AS Probability, 
                            COUNT(DISTINCT MatchId) AS TotalMatches, MIN(MatchStartTime) AS FirstPlayed,  MAX(MatchStartTime) AS LastPlayed,
                            stats.PlayerId, stats.PlayerRoute, stats.TeamId, stats.TeamName
                            FROM {Tables.PlayerIdentity} pi INNER JOIN {Tables.PlayerInMatchStatistics} AS stats ON pi.PlayerIdentityId = stats.PlayerIdentityId
                            <<WHERE>>
                            GROUP BY stats.PlayerId, stats.PlayerRoute, stats.PlayerIdentityId, pi.PlayerIdentityName, pi.RouteSegment, stats.TeamId, stats.TeamName 
                            ORDER BY stats.TeamId ASC, {PROBABILITY_CALCULATION} DESC, pi.PlayerIdentityName ASC";

                var (where, parameters) = BuildWhereClause(filter);
                sql = sql.Replace("<<WHERE>>", $"WHERE 1=1 {where}");

                var identities = (await connection.QueryAsync<PlayerIdentity, Player, Team, PlayerIdentity>(sql,
                    (identity, player, team) =>
                    {
                        identity.Team = team;
                        identity.Player = player;
                        return identity;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "PlayerId, TeamId").ConfigureAwait(false)).ToList();

                // Populate the PlayerIdentities collections of the players with the data that we have
                foreach (var identity in identities)
                {
                    identity.Player!.PlayerIdentities = identities.Where(x => x.Player?.PlayerId == identity.Player.PlayerId).ToList();
                }

                return identities;
            }
        }

        /// <summary> 
        /// Adds standard filters to the WHERE clause
        /// </summary> 
        private (string where, Dictionary<string, object> parameters) BuildWhereClause(PlayerFilter? filter)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (filter?.PlayerIds?.Count > 0)
            {
                where.Add("stats.PlayerId IN @PlayerIds");
                parameters.Add("@PlayerIds", filter.PlayerIds.Select(x => x.ToString()));
            }

            if (filter?.PlayerIdentityIds?.Count > 0)
            {
                where.Add("stats.PlayerIdentityId IN @PlayerIdentityIds");
                parameters.Add("@PlayerIdentityIds", filter.PlayerIdentityIds.Select(x => x.ToString()));
            }

            if (!string.IsNullOrEmpty(filter?.Query))
            {
                where.Add("stats.PlayerIdentityName LIKE @Query");
                parameters.Add("@Query", $"%{filter.Query.Replace(" ", "%")}%");
            }

            if (filter?.ClubIds?.Count > 0)
            {
                where.Add("stats.ClubId IN @ClubIds");
                parameters.Add("@ClubIds", filter.ClubIds.Select(x => x.ToString()));
            }

            if (filter?.TeamIds?.Count > 0)
            {
                where.Add("stats.TeamId IN @TeamIds");
                parameters.Add("@TeamIds", filter.TeamIds.Select(x => x.ToString()));
            }

            if (filter?.MatchLocationIds?.Count > 0)
            {
                where.Add("stats.MatchLocationId IN @MatchLocationIds");
                parameters.Add("@MatchLocationIds", filter.MatchLocationIds.Select(x => x.ToString()));
            }

            if (filter?.CompetitionIds?.Count > 0)
            {
                where.Add("stats.CompetitionId IN @CompetitionIds");
                parameters.Add("@CompetitionIds", filter.CompetitionIds.Select(x => x.ToString()));
            }

            if (filter?.SeasonIds?.Count > 0)
            {
                where.Add("stats.SeasonId IN @SeasonIds");
                parameters.Add("@SeasonIds", filter.SeasonIds.Select(x => x.ToString()));
            }

            return (where.Count > 0 ? " AND " + string.Join(" AND ", where) : string.Empty, parameters);
        }

        /// <inheritdoc />
        public async Task<Player?> ReadPlayerByRoute(string route, StatisticsFilter? filter = null)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "players");

            if (filter == null) { filter = new StatisticsFilter(); }
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            parameters.Add("Route", normalisedRoute);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                // Get Player and PlayerIdentity data from the original tables rather than PlayerInMatchStatistics because the original tables
                // will be updated when a member associates their account with a player, and we need to see the change immediately.
                // Updates to PlayerInMatchStatistics are done asynchronously and the data will not be updated by the time this is called again.

                var playerData = await connection.QueryAsync<Player, PlayerIdentity, Team, Player>(
                    $@"SELECT p.PlayerId, p.PlayerRoute, p.MemberKey,
                        pi.PlayerIdentityId, pi.PlayerIdentityName,
                        (SELECT COUNT(DISTINCT MatchId) AS TotalMatches FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = pi.PlayerIdentityId {where}) AS TotalMatches,
                        (SELECT MIN(MatchStartTime) AS FirstPlayed FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = pi.PlayerIdentityId {where}) AS FirstPlayed,
                        (SELECT MAX(MatchStartTime) AS LastPlayed FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = pi.PlayerIdentityId {where}) AS LastPlayed,
                        t.TeamId, tv.TeamName, t.TeamRoute
                        FROM {Tables.Player} p INNER JOIN {Tables.PlayerIdentity} pi ON p.PlayerId = pi.PlayerId
                        INNER JOIN {Tables.Team} t ON pi.TeamId = t.TeamId
                        INNER JOIN {Tables.TeamVersion} tv ON t.TeamId = tv.TeamId
                        WHERE LOWER(PlayerRoute) = @Route
                        AND tv.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                        (player, playerIdentity, team) =>
                        {
                            playerIdentity.Team = team;
                            player.PlayerIdentities.Add(playerIdentity);
                            return player;
                        },
                        parameters,
                        splitOn: "PlayerIdentityId, TeamId"
                        ).ConfigureAwait(false);

                var playerToReturn = playerData.GroupBy(x => x.PlayerId).Select(group =>
                {
                    var player = group.First();
                    player.PlayerIdentities = group.Select(x => x.PlayerIdentities.Single()).OfType<PlayerIdentity>().ToList();
                    return player;
                }).FirstOrDefault();

                return playerToReturn;
            }
        }

        /// <inheritdoc />
        public async Task<Player?> ReadPlayerByMemberKey(Guid key)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<Player>(
                    $@"SELECT TOP 1 PlayerRoute
                        FROM {Tables.Player}
                        WHERE MemberKey = @MemberKey",
                        new { MemberKey = key });
            }
        }

        /// <inheritdoc />
        public async Task<PlayerIdentity?> ReadPlayerIdentityByRoute(string route)
        {
            var normalisedRouteForTeam = _routeNormaliser.NormaliseRouteToEntity(route, "/teams");

            var playerIdentitySegmentParsed = false;
            var playerIdentitySegment = route.Substring(route.IndexOf(normalisedRouteForTeam) + normalisedRouteForTeam.Length);
            if (playerIdentitySegment.StartsWith("/edit/players/"))
            {
                playerIdentitySegment = playerIdentitySegment.Substring(14);
                var pos = playerIdentitySegment.IndexOf("/");
                if (pos > -1)
                {
                    playerIdentitySegment = playerIdentitySegment.Substring(0, pos);
                    playerIdentitySegmentParsed = true;
                }
            }
            if (!playerIdentitySegmentParsed)
            {
                throw new ArgumentException($"{nameof(route)} was not in the expected format");
            }

            var parameters = new Dictionary<string, object> {
                { "RouteSegment", playerIdentitySegment },
                { "TeamRoute", normalisedRouteForTeam }
            };

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<PlayerIdentity, Team, PlayerIdentity>(
                    $@"SELECT pi.PlayerIdentityId, pi.PlayerIdentityName,
                              t.TeamId, tv.TeamName, t.TeamRoute 
                       FROM {Tables.PlayerIdentity} pi INNER JOIN {Tables.Team} t ON pi.TeamId = t.TeamId 
                       INNER JOIN {Tables.TeamVersion} tv ON t.TeamId = tv.TeamId
                       WHERE LOWER(pi.RouteSegment) = @RouteSegment AND LOWER(t.TeamRoute) = @TeamRoute
                       AND tv.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                    (identity, team) =>
                    {
                        identity.Team = team;
                        return identity;
                    },
                    parameters,
                    splitOn: "TeamId"
                    ).ConfigureAwait(false);

                return results.SingleOrDefault();
            }

        }
    }
}
