using Dapper;
using Stoolball.Clubs;
using Stoolball.Routing;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Clubs
{
    /// <summary>
    /// Gets stoolball club data from the Umbraco database
    /// </summary>
    public class SqlServerClubDataSource : IClubDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerClubDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single stoolball club based on its route
        /// </summary>
        /// <param name="route">/clubs/example-club</param>
        /// <returns>A matching <see cref="Club"/> or <c>null</c> if not found</returns>
        public async Task<Club> ReadClubByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "clubs");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var clubs = await connection.QueryAsync<Club, Team, Club>(
                        $@"SELECT c.ClubId, cn.ClubName, c.ClubMark, c.MemberGroupId, c.MemberGroupName, c.ClubRoute,
                            t.TeamId, tn.TeamName, t.TeamRoute, t.UntilYear
                            FROM {Tables.Club} AS c 
                            INNER JOIN {Tables.ClubName} AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                            LEFT JOIN {Tables.Team} AS t ON c.ClubId = t.ClubId
                            LEFT JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            WHERE LOWER(c.ClubRoute) = @Route
                            ORDER BY tn.TeamName",
                        (club, team) =>
                        {
                            club.Teams.Add(team);
                            return club;
                        },
                        new { Route = normalisedRoute },
                        splitOn: "TeamId").ConfigureAwait(false);

                    var resolvedClub = clubs.GroupBy(club => club.ClubId).Select(group =>
                    {
                        var groupedClub = group.First();
                        groupedClub.Teams = group.Select(club => club.Teams.Single()).OfType<Team>().ToList();
                        return groupedClub;
                    }).FirstOrDefault();

                    return resolvedClub;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerClubDataSource), ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of clubs based on a query
        /// </summary>
        /// <returns>A list of <see cref="Club"/> objects. An empty list if no clubs are found.</returns>
        public async Task<List<Club>> ReadClubListings(ClubQuery clubQuery)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var sql = $@"SELECT c.ClubId, cn.ClubName, c.ClubRoute,
                            t.PlayerType
                            FROM {Tables.Club} AS c 
                            INNER JOIN {Tables.ClubName} AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                            LEFT JOIN {Tables.Team} AS t ON c.ClubId = t.ClubId AND t.UntilYear IS NULL
                            <<WHERE>>
                            ORDER BY cn.ClubName";

                    var where = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(clubQuery?.Query))
                    {
                        where.Add("cn.ClubName LIKE @Query");
                        parameters.Add("@Query", $"%{clubQuery.Query}%");
                    }

                    sql = sql.Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty);

                    var clubs = await connection.QueryAsync<Club, Team, Club>(sql,
                        (club, team) =>
                        {
                            if (team != null)
                            {
                                club.Teams.Add(team);
                            }
                            return club;
                        },
                        new DynamicParameters(parameters),
                        splitOn: "PlayerType").ConfigureAwait(false);

                    var resolvedClubs = clubs.GroupBy(club => club.ClubId).Select(copiesOfClub =>
                    {
                        var resolvedClub = copiesOfClub.First();
                        resolvedClub.Teams = copiesOfClub.Select(club => club.Teams.SingleOrDefault()).OfType<Team>().ToList();
                        return resolvedClub;
                    }).ToList();

                    return resolvedClubs;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerClubDataSource), ex);
                throw;
            }
        }
    }
}
