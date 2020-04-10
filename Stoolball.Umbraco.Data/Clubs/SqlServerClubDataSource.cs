using Dapper;
using Stoolball.Clubs;
using Stoolball.Routing;
using Stoolball.Teams;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;

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
        /// <param name="route">clubs/example-club</param>
        /// <returns>A matching <see cref="Club"/> or <c>null</c> if not found</returns>
        public async Task<Club> ReadClubByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "clubs");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var clubs = await connection.QueryAsync<Club, Team, Club>(
                        $@"SELECT c.ClubId, cn.ClubName, c.HowManyPlayers, c.PlaysOutdoors, c.PlaysIndoors,
                            c.Twitter, c.Facebook, c.Instagram, c.YouTube, c.Website, c.ClubMark, c.ClubRoute,
                            tn.TeamName, t.TeamRoute
                            FROM {Constants.Tables.Club} AS c 
                            INNER JOIN {Constants.Tables.ClubName} AS cn ON c.ClubId = cn.ClubId
                            LEFT JOIN {Constants.Tables.Team} AS t ON c.ClubId = t.ClubId
                            LEFT JOIN {Constants.Tables.TeamName} AS tn ON t.TeamId = tn.TeamId
                            WHERE LOWER(c.ClubRoute) = @Route AND cn.UntilDate IS NULL",
                        (club, team) =>
                        {
                            club.Teams.Add(team);
                            return club;
                        },
                        new { Route = normalisedRoute },
                        splitOn: "TeamName").ConfigureAwait(false);

                    var resolvedClub = clubs.GroupBy(club => club.ClubId).Select(group =>
                    {
                        var groupedClub = group.First();
                        groupedClub.Teams = group.Select(club => club.Teams.Single()).ToList();
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
    }
}
