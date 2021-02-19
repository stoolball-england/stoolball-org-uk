using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Routing;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball player data from the Umbraco database
    /// </summary>
    public class SqlServerPlayerDataSource : IPlayerDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerPlayerDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a list of player identities based on a query
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no player identities are found.</returns>
        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerIdentityQuery playerQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = $@"SELECT p.PlayerIdentityId, p.PlayerIdentityName, p.TotalMatches, p.FirstPlayed, p.LastPlayed,
                            t.TeamId, tn.TeamName
                            FROM {Tables.PlayerIdentity} AS p 
                            INNER JOIN {Tables.Team} AS t ON p.TeamId = t.TeamId
                            INNER JOIN {Tables.TeamVersion} AS tn ON t.TeamId = tn.TeamId
                            <<WHERE>>
                            AND tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY t.TeamId ASC, p.Probability DESC, p.PlayerIdentityName ASC";

                var where = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(playerQuery?.Query))
                {
                    where.Add("p.PlayerIdentityName LIKE @Query");
                    parameters.Add("@Query", $"%{playerQuery.Query.Replace(" ", "%")}%");
                }

                if (playerQuery?.TeamIds?.Count > 0)
                {
                    where.Add("p.TeamId IN @TeamIds");
                    parameters.Add("@TeamIds", playerQuery.TeamIds.Select(x => x.ToString()));
                }

                sql = sql.Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "WHERE 1=1"); // Ensure there's always a WHERE clause so that it can be appended to

                return (await connection.QueryAsync<PlayerIdentity, Team, PlayerIdentity>(sql,
                    (player, team) =>
                    {
                        player.Team = team;
                        return player;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "TeamId").ConfigureAwait(false)).ToList();
            }
        }

        /// <inheritdoc />
        public async Task<Player> ReadPlayerByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "players");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var playerData = await connection.QueryAsync<Player, PlayerIdentity, Team, Player>(
                    $@"SELECT PlayerId, PlayerIdentityName AS PlayerName,
                        PlayerIdentityId, PlayerIdentityName, COUNT(DISTINCT MatchId) AS TotalMatches, MIN(MatchStartTime) AS FirstPlayed, MAX(MatchStartTime) AS LastPlayed, 
                        TeamName, TeamRoute
                        FROM {Tables.PlayerInMatchStatistics} 
                        WHERE LOWER(PlayerRoute) = @Route
                        GROUP BY PlayerId, PlayerIdentityId, PlayerIdentityName, TeamName, TeamRoute",
                        (player, playerIdentity, team) =>
                        {
                            playerIdentity.Team = team;
                            player.PlayerIdentities.Add(playerIdentity);
                            return player;
                        },
                        new { Route = normalisedRoute },
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
    }
}
