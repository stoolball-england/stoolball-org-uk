using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Routing;
using Stoolball.Teams;

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
        public async Task<Club?> ReadClubByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "clubs");

            return (await SelectClubsWhere("LOWER(c.ClubRoute) = @Route", new { Route = normalisedRoute }).ConfigureAwait(false)).FirstOrDefault();
        }

        /// <summary>
        /// Gets a list of clubs based on a query
        /// </summary>
        /// <returns>A list of <see cref="Club"/> objects. An empty list if no clubs are found.</returns>
        public async Task<List<Club>> ReadClubs(ClubFilter? filter)
        {
            filter = filter ?? new ClubFilter();

            if (filter.TeamIds.Any())
            {
                return await SelectClubsWhere($"c.ClubId IN (SELECT ClubId FROM {Tables.Team} WHERE TeamId IN @TeamIds)", new { filter.TeamIds }).ConfigureAwait(false);
            }
            else
            {
                return await SelectClubsWhere("1=1", null).ConfigureAwait(false);
            }
        }

        private async Task<List<Club>> SelectClubsWhere(string where, object? parameters)
        {

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var clubs = await connection.QueryAsync($@"SELECT c.ClubId, cn.ClubName, c.MemberGroupKey, c.MemberGroupName, c.ClubRoute,
                            t.TeamId, tn.TeamName, t.TeamRoute, YEAR(tn.UntilDate) AS UntilYear, t.ClubMark
                            FROM {Tables.Club} AS c 
                            INNER JOIN {Tables.ClubVersion} AS cn ON c.ClubId = cn.ClubId
                            LEFT JOIN {Tables.Team} AS t ON c.ClubId = t.ClubId
                            LEFT JOIN {Tables.TeamVersion} AS tn ON t.TeamId = tn.TeamId
                            WHERE {where}
                            AND cn.ClubVersionId = (SELECT TOP 1 ClubVersionId FROM {Tables.ClubVersion} WHERE ClubId = c.ClubId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            AND (tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR tn.TeamVersionId IS NULL)
                            ORDER BY CASE WHEN tn.UntilDate IS NULL THEN 0 ELSE 1 END, tn.TeamName",
                        (Func<Club, Team, Club>)((club, team) =>
                        {
                            club.Teams.Add(team);
                            return club;
                        }),
                        parameters,
                        splitOn: "TeamId").ConfigureAwait(false);

                var resolvedClubs = clubs.GroupBy(club => club.ClubId).Select(group =>
                {
                    var groupedClub = group.First();
                    groupedClub.Teams = group.Select(club => club.Teams.Single()).OfType<Team>().ToList();
                    return groupedClub;
                });

                return resolvedClubs.ToList();
            }
        }
    }
}