using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
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
        public async Task<List<Player>> ReadPlayers(PlayerFilter filter)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await ReadPlayers(filter, connection).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public async Task<List<Player>> ReadPlayers(PlayerFilter filter, IDbConnection connection)
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
                                PlayerIdentityId, PlayerIdentityName, MIN(MatchStartTime) AS FirstPlayed,  MAX(MatchStartTime) AS LastPlayed, 
                                TeamName 
                                FROM {Tables.PlayerInMatchStatistics} AS stats 
                                <<WHERE>>
                                GROUP BY stats.PlayerId, stats.PlayerRoute, stats.PlayerIdentityId, stats.PlayerIdentityName, stats.TeamName";

            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (filter?.PlayerIds?.Count > 0)
            {
                where.Add("stats.PlayerId IN @PlayerIds");
                parameters.Add("@PlayerIds", filter.PlayerIds.Select(x => x.ToString()));
            }

            sql = sql.Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty);

            var rawResults = (await connection.QueryAsync<Player, PlayerIdentity, Team, Player>(sql,
                (player, identity, team) =>
                {
                    identity.Team = team;
                    identity.Player = player;
                    player.PlayerIdentities.Add(identity);
                    return player;
                },
                new DynamicParameters(parameters),
                splitOn: "PlayerIdentityId, TeamName").ConfigureAwait(false)).ToList();

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
        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerFilter filter)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = $@"SELECT stats.PlayerIdentityId, stats.PlayerIdentityName, stats.Probability, 
                            COUNT(DISTINCT MatchId) AS TotalMatches, MIN(MatchStartTime) AS FirstPlayed,  MAX(MatchStartTime) AS LastPlayed,
                            stats.PlayerId, stats.PlayerRoute, stats.TeamId, stats.TeamName
                            FROM {Tables.PlayerInMatchStatistics} AS stats 
                            <<WHERE>>
                            GROUP BY stats.PlayerId, stats.PlayerRoute, stats.PlayerIdentityId, stats.PlayerIdentityName, stats.TeamId, stats.TeamName, stats.Probability
                            ORDER BY stats.TeamId ASC, Probability DESC, stats.PlayerIdentityName ASC";

                var where = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(filter?.Query))
                {
                    where.Add("stats.PlayerIdentityName LIKE @Query");
                    parameters.Add("@Query", $"%{filter.Query.Replace(" ", "%")}%");
                }

                if (filter?.TeamIds?.Count > 0)
                {
                    where.Add("stats.TeamId IN @TeamIds");
                    parameters.Add("@TeamIds", filter.TeamIds.Select(x => x.ToString()));
                }

                if (filter?.PlayerIdentityIds?.Count > 0)
                {
                    where.Add("stats.PlayerIdentityId IN @PlayerIdentityIds");
                    parameters.Add("@PlayerIdentityIds", filter.PlayerIdentityIds.Select(x => x.ToString()));
                }

                sql = sql.Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty);

                return (await connection.QueryAsync<PlayerIdentity, Player, Team, PlayerIdentity>(sql,
                    (identity, player, team) =>
                    {
                        identity.Team = team;
                        identity.Player = player;
                        return identity;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "PlayerId, TeamId").ConfigureAwait(false)).ToList();
            }
        }

        /// <inheritdoc />
        public async Task<Player> ReadPlayerByRoute(string route, StatisticsFilter filter = null)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "players");

            if (filter == null) { filter = new StatisticsFilter(); }
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            parameters.Add("Route", normalisedRoute);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                // Get MemberKey from the Player table rather than copying it to PlayerInMatchStatistics because it will be updated when
                // a member associates their account with a player. If there are many PlayerInMatchStatistics records to update this will 
                // take time and should not be done in the response, yet the response (and responses to other requests with filters applied)
                // will cache the result of this method so it must include the new value immediately.

                // This assumes it should only be needed here, not in other statistics queries.
                var playerData = await connection.QueryAsync<Player, PlayerIdentity, Team, Player>(
                    $@"SELECT PlayerId, PlayerRoute, (SELECT MemberKey FROM {Tables.Player} WHERE PlayerRoute = @Route) AS MemberKey,
                        PlayerIdentityId, PlayerIdentityName, 
                        (SELECT COUNT(DISTINCT MatchId) AS TotalMatches FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = identities.PlayerIdentityId {where}) AS TotalMatches,
                        (SELECT MIN(MatchStartTime) AS FirstPlayed FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = identities.PlayerIdentityId {where}) AS FirstPlayed,
                        (SELECT MAX(MatchStartTime) AS LastPlayed FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = identities.PlayerIdentityId {where}) AS LastPlayed,
                        TeamName, TeamRoute
                        FROM {Tables.PlayerInMatchStatistics} identities
                        WHERE LOWER(PlayerRoute) = @Route
                        GROUP BY PlayerId, PlayerRoute, PlayerIdentityId, PlayerIdentityName, TeamName, TeamRoute",
                        (player, playerIdentity, team) =>
                        {
                            playerIdentity.Team = team;
                            player.PlayerIdentities.Add(playerIdentity);
                            return player;
                        },
                        parameters,
                        splitOn: "PlayerIdentityId, TeamName"
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
        public async Task<Player> ReadPlayerByMemberKey(Guid key)
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
    }
}
