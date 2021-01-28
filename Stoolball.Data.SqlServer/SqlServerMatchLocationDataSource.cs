using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets match location data from the Umbraco database
    /// </summary>
    public class SqlServerMatchLocationDataSource : IMatchLocationDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerMatchLocationDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single match location based on its route
        /// </summary>
        /// <param name="route">/locations/example-location</param>
        /// <param name="includeRelated"><c>true</c> to include the teams based at the selected location; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="MatchLocation"/> or <c>null</c> if not found</returns>
        public async Task<MatchLocation> ReadMatchLocationByRoute(string route, bool includeRelated = false)
        {
            return await (includeRelated ? ReadMatchLocationWithRelatedDataByRoute(route) : ReadMatchLocationByRoute(route)).ConfigureAwait(false);
        }

        private async Task<MatchLocation> ReadMatchLocationByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "locations");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var locations = await connection.QueryAsync<MatchLocation>(
                    $@"SELECT ml.MatchLocationId, ml.MatchLocationRoute, ml.Latitude, ml.Longitude, ml.GeoPrecision, ml.MatchLocationNotes, ml.MemberGroupName,
                            ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.StreetDescription, ml.Locality, ml.Town, ml.AdministrativeArea, ml.Postcode
                            FROM {Tables.MatchLocation} AS ml
                            WHERE LOWER(ml.MatchLocationRoute) = @Route",
                    new { Route = normalisedRoute }).ConfigureAwait(false);

                return locations.FirstOrDefault();
            }
        }

        private async Task<MatchLocation> ReadMatchLocationWithRelatedDataByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "locations");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var locations = await connection.QueryAsync<MatchLocation, Team, MatchLocation>(
                    $@"SELECT ml.MatchLocationId, ml.MatchLocationNotes, ml.MatchLocationRoute, ml.MemberGroupKey, ml.MemberGroupName,
                            ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.StreetDescription, ml.Locality, ml.Town, ml.AdministrativeArea, ml.Postcode, 
                            ml.Latitude, ml.Longitude, ml.GeoPrecision,
                            t.TeamId, tn.TeamName, t.TeamRoute
                            FROM {Tables.MatchLocation} AS ml
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml ON ml.MatchLocationId = tml.MatchLocationId AND tml.UntilDate IS NULL
                            LEFT JOIN {Tables.Team} AS t ON tml.TeamId = t.TeamId AND t.UntilYear IS NULL AND NOT t.TeamType = '{TeamType.Transient}'
                            LEFT JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            WHERE LOWER(ml.MatchLocationRoute) = @Route
                            ORDER BY tn.TeamName",
                    (matchLocation, team) =>
                    {
                        matchLocation.Teams.Add(team);
                        return matchLocation;
                    },
                    new { Route = normalisedRoute },
                    splitOn: "TeamId").ConfigureAwait(false);

                var locationToReturn = locations.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (locationToReturn != null)
                {
                    locationToReturn.Teams = locations.Select(location => location.Teams.Single()).OfType<Team>().ToList();
                }

                return locationToReturn;
            }
        }

        /// <summary>
        /// Gets the number of match locations that match a query
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalMatchLocations(MatchLocationQuery matchLocationQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (where, parameters) = BuildWhereClause(matchLocationQuery);
                return await connection.ExecuteScalarAsync<int>($@"SELECT COUNT(MatchLocationId)
                            FROM {Tables.MatchLocation} AS ml
                            {where}", new DynamicParameters(parameters)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a list of match locations based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchLocation"/> objects. An empty list if no match locations are found.</returns>
        public async Task<List<MatchLocation>> ReadMatchLocations(MatchLocationQuery matchLocationQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                // order by clause places locations with active teams above those which have none

                var (where, parameters) = BuildWhereClause(matchLocationQuery);

                var sql = $@"SELECT ml2.MatchLocationId, ml2.MatchLocationRoute,
                            ml2.SecondaryAddressableObjectName, ml2.PrimaryAddressableObjectName, ml2.Locality, ml2.Town,
                            t2.PlayerType
                            FROM {Tables.MatchLocation} AS ml2
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml2 ON ml2.MatchLocationId = tml2.MatchLocationId AND tml2.UntilDate IS NULL
                            LEFT JOIN {Tables.Team} AS t2 ON tml2.TeamId = t2.TeamId AND t2.UntilYear IS NULL
                            WHERE ml2.MatchLocationId IN (
                                SELECT ml.MatchLocationId
                                FROM {Tables.MatchLocation} AS ml 
                                {where}
                                ORDER BY 
                                    CASE WHEN (
                                        SELECT COUNT(t.TeamId) FROM {Tables.TeamMatchLocation} AS tml 
                                        INNER JOIN {Tables.Team} AS t ON tml.TeamId = t.TeamId AND t.UntilYear IS NULL AND tml.UntilDate IS NULL
                                        WHERE ml.MatchLocationId = tml.MatchLocationId 
                                    ) > 0 THEN 0 ELSE 1 END,
                                ml.SortName
                                OFFSET {(matchLocationQuery.PageNumber - 1) * matchLocationQuery.PageSize} ROWS FETCH NEXT {matchLocationQuery.PageSize} ROWS ONLY)
                            ORDER BY 
                                CASE WHEN (
                                    SELECT COUNT(t3.TeamId) FROM {Tables.TeamMatchLocation} AS tml3 
                                    INNER JOIN {Tables.Team} AS t3 ON tml3.TeamId = t3.TeamId AND t3.UntilYear IS NULL AND tml3.UntilDate IS NULL 
                                    WHERE ml2.MatchLocationId = tml3.MatchLocationId 
                                ) > 0 THEN 0 ELSE 1 END,
                            ml2.SortName";

                var locations = await connection.QueryAsync<MatchLocation, Team, MatchLocation>(sql,
                    (location, team) =>
                    {
                        if (team != null)
                        {
                            location.Teams.Add(team);
                        }
                        return location;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "PlayerType").ConfigureAwait(false);

                var resolvedLocations = locations.GroupBy(location => location.MatchLocationId).Select(copiesOfLocation =>
                {
                    var resolvedLocation = copiesOfLocation.First();
                    resolvedLocation.Teams = copiesOfLocation.Select(location => location.Teams.SingleOrDefault()).OfType<Team>().ToList();
                    return resolvedLocation;
                }).ToList();

                return resolvedLocations;
            }
        }

        private static (string sql, Dictionary<string, object> parameters) BuildWhereClause(MatchLocationQuery matchLocationQuery)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(matchLocationQuery?.Query))
            {
                where.Add(@"(ml.SecondaryAddressableObjectName LIKE @Query OR 
                            ml.PrimaryAddressableObjectName LIKE @Query OR 
                            ml.Locality LIKE @Query OR
                            ml.Town LIKE @Query)");
                parameters.Add("@Query", $"%{matchLocationQuery.Query}%");
            }

            if (matchLocationQuery?.ExcludeMatchLocationIds?.Count > 0)
            {
                where.Add("ml.MatchLocationId NOT IN @ExcludeMatchLocationIds");
                parameters.Add("@ExcludeMatchLocationIds", matchLocationQuery.ExcludeMatchLocationIds.Select(x => x.ToString()));
            }

            return (where.Count > 0 ? $@"WHERE " + string.Join(" AND ", where) : string.Empty, parameters);
        }
    }
}
