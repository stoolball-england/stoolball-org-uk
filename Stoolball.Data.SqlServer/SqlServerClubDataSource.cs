using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Clubs;
using Stoolball.Routing;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball club data from the Umbraco database
    /// </summary>
    public class SqlServerClubDataSource : IClubDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerClubDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single stoolball club based on its route
        /// </summary>
        /// <param name="route">/clubs/example-club</param>
        /// <returns>A matching <see cref="Club"/> or <c>null</c> if not found</returns>
        public async Task<Club> ReadClubByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "clubs");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var clubs = await connection.QueryAsync<Club, Team, Club>(
                    $@"SELECT c.ClubId, cn.ClubName, c.MemberGroupKey, c.MemberGroupName, c.ClubRoute,
                            t.TeamId, tn.TeamName, t.TeamRoute, YEAR(tn.UntilDate) AS UntilYear, t.ClubMark
                            FROM {Tables.Club} AS c 
                            INNER JOIN {Tables.ClubName} AS cn ON c.ClubId = cn.ClubId
                            LEFT JOIN {Tables.Team} AS t ON c.ClubId = t.ClubId
                            LEFT JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId
                            WHERE LOWER(c.ClubRoute) = @Route
                            AND cn.ClubNameId = (SELECT TOP 1 ClubNameId FROM {Tables.ClubName} WHERE ClubId = c.ClubId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            AND tn.TeamNameId = (SELECT TOP 1 TeamNameId FROM {Tables.TeamName} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY CASE WHEN tn.UntilDate IS NULL THEN 0 ELSE 1 END, tn.TeamName",
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
    }
}
