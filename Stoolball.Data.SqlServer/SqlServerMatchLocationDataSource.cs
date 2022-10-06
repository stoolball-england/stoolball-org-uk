using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets match location data from the Umbraco database
    /// </summary>
    public class SqlServerMatchLocationDataSource : IMatchLocationDataSource, ICacheableMatchLocationDataSource
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
        public async Task<MatchLocation?> ReadMatchLocationByRoute(string route, bool includeRelated = false)
        {
            return await (includeRelated ? ReadMatchLocationWithRelatedDataByRoute(route) : ReadMatchLocationByRoute(route)).ConfigureAwait(false);
        }

        private async Task<MatchLocation?> ReadMatchLocationByRoute(string route)
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

        private async Task<MatchLocation?> ReadMatchLocationWithRelatedDataByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "locations");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var locations = await connection.QueryAsync<MatchLocation, Team, MatchLocation>(
                    $@"SELECT ml.MatchLocationId, ml.MatchLocationNotes, ml.MatchLocationRoute, ml.MemberGroupKey, ml.MemberGroupName,
                            ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.StreetDescription, ml.Locality, ml.Town, ml.AdministrativeArea, ml.Postcode, 
                            ml.Latitude, ml.Longitude, ml.GeoPrecision,
                            t.TeamId, YEAR(tn.UntilDate) AS UntilYear, tn.TeamName, t.TeamRoute
                            FROM {Tables.MatchLocation} AS ml
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml ON ml.MatchLocationId = tml.MatchLocationId
                            LEFT JOIN {Tables.Team} AS t ON tml.TeamId = t.TeamId AND NOT t.TeamType = '{TeamType.Transient}'
                            LEFT JOIN {Tables.TeamVersion} AS tn ON t.TeamId = tn.TeamId
                            WHERE LOWER(ml.MatchLocationRoute) = @Route
                            AND tml.UntilDate IS NULL                            
                            AND (tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR tn.TeamVersionId IS NULL)
                            ORDER BY CASE WHEN tn.UntilDate IS NULL THEN 0 ELSE 1 END, tn.TeamName",
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
        public async Task<int> ReadTotalMatchLocations(MatchLocationFilter? filter)
        {
            if (filter == null) filter = new MatchLocationFilter();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (sql, parameters) = BuildMatchLocationQuery(filter, $@"SELECT COUNT(DISTINCT ml.MatchLocationId)
                            FROM {Tables.MatchLocation} AS ml <<JOIN>> <<WHERE>>", Array.Empty<string>());
                return await connection.ExecuteScalarAsync<int>(sql, new DynamicParameters(parameters)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a list of match locations based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchLocation"/> objects. An empty list if no match locations are found.</returns>
        public async Task<List<MatchLocation>> ReadMatchLocations(MatchLocationFilter? filter)
        {
            if (filter == null) filter = new MatchLocationFilter();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                // order by clause places locations with active teams above those which have none
                var sql = $@"SELECT ml2.MatchLocationId, ml2.MatchLocationRoute, ml2.Latitude, ml2.Longitude,
                            ml2.SecondaryAddressableObjectName, ml2.PrimaryAddressableObjectName, ml2.Locality, ml2.Town,
                            t2.TeamId, tv2.TeamName, t2.TeamRoute, t2.TeamType, t2.PlayerType, YEAR(tv2.UntilDate) AS UntilYear
                            FROM {Tables.MatchLocation} AS ml2
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml2 ON ml2.MatchLocationId = tml2.MatchLocationId
                            LEFT JOIN {Tables.Team} AS t2 ON tml2.TeamId = t2.TeamId 
                            LEFT JOIN {Tables.TeamVersion} AS tv2 ON t2.TeamId = tv2.TeamId
                            <<JOIN-T2-SEASON>>
                            WHERE tml2.UntilDate IS NULL
                            AND (tv2.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t2.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR tv2.TeamVersionId IS NULL)
                            <<WHERE-T2-TYPE>>
                            <<WHERE-T2-SEASON>>
                            AND ml2.MatchLocationId IN (

                               SELECT MatchLocationId FROM (
                                    SELECT DISTINCT ml.MatchLocationId, ml.ComparableName,
                                        CASE WHEN (
                                                SELECT COUNT(t4.TeamId) FROM {Tables.TeamMatchLocation} AS tml4
                                                LEFT JOIN {Tables.Team} AS t4 ON tml4.TeamId = t4.TeamId
                                                LEFT JOIN {Tables.TeamVersion} AS tv4 ON t4.TeamId = tv4.TeamId
                                                WHERE tml4.MatchLocationId = ml.MatchLocationId
                                                AND (tv4.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t4.TeamId ORDER BY ISNULL(UntilDate, '9999-12-31') DESC) OR tv4.TeamVersionId IS NULL)
                                                AND tv4.UntilDate IS NULL 
                                                AND tml4.UntilDate IS NULL
                                                <<WHERE-T4-TYPE>>
                                            ) > 0 THEN 1 ELSE 0 END AS HasActiveTeams
                                
                                        FROM {Tables.MatchLocation} AS ml 
                                        LEFT JOIN {Tables.TeamMatchLocation} AS tml ON ml.MatchLocationId = tml.MatchLocationId
                                        LEFT JOIN {Tables.Team} AS t ON tml.TeamId = t.TeamId
                                        LEFT JOIN {Tables.TeamVersion} AS tv ON t.TeamId = tv.TeamId
                                        <<JOIN>>
                                        <<WHERE>>
                                        AND tml.UntilDate IS NULL

                                ) AS DistinctIdsForPaging 
                                    ORDER BY HasActiveTeams DESC, ComparableName
                                    OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY
                            )
                            ORDER BY 
                                CASE WHEN (
                                    SELECT COUNT(t3.TeamId) FROM {Tables.TeamMatchLocation} AS tml3 
                                    LEFT JOIN {Tables.Team} AS t3 ON tml3.TeamId = t3.TeamId
                                    LEFT JOIN {Tables.TeamVersion} AS tv3 ON t3.TeamId = tv3.TeamId
                                    WHERE tml3.MatchLocationId = ml2.MatchLocationId
                                    AND (tv3.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t3.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR tv3.TeamVersionId IS NULL)
                                    AND tv3.UntilDate IS NULL
                                    AND tml3.UntilDate IS NULL 
                                    <<WHERE-T3-TYPE>>
                                ) > 0 THEN 1 ELSE 0 END DESC,
                            ml2.ComparableName";

                var (filteredSql, parameters) = BuildMatchLocationQuery(filter, sql, new[] { Tables.TeamMatchLocation, Tables.Team, Tables.TeamVersion });

                parameters.Add("@PageOffset", (filter.Paging.PageNumber - 1) * filter.Paging.PageSize);
                parameters.Add("@PageSize", filter.Paging.PageSize);

                filteredSql = filteredSql.Replace("<<WHERE-T2-TYPE>>", filter.TeamTypes.Count > 0 ? "AND t2.TeamType IN @TeamTypes" : string.Empty);
                filteredSql = filteredSql.Replace("<<WHERE-T3-TYPE>>", filter.TeamTypes.Count > 0 ? "AND t3.TeamType IN @TeamTypes" : string.Empty);
                filteredSql = filteredSql.Replace("<<WHERE-T4-TYPE>>", filter.TeamTypes.Count > 0 ? "AND t4.TeamType IN @TeamTypes" : string.Empty);

                // when filtering by season, return only the teams in the season in case there are other teams also based at the same locations
                filteredSql = filteredSql.Replace("<<JOIN-T2-SEASON>>", filter.SeasonIds.Count > 0 ? $"LEFT JOIN {Tables.SeasonTeam} AS st2 ON tml2.TeamId = st2.TeamId" : string.Empty);
                filteredSql = filteredSql.Replace("<<WHERE-T2-SEASON>>", filter.SeasonIds.Count > 0 ? "AND st2.SeasonId IN @SeasonIds" : string.Empty);

                var locations = await connection.QueryAsync<MatchLocation, Team, MatchLocation>(filteredSql,
                              (location, team) =>
                              {
                                  if (team != null)
                                  {
                                      location.Teams.Add(team);
                                  }
                                  return location;
                              },
                              new DynamicParameters(parameters),
                              splitOn: "TeamId").ConfigureAwait(false);

                var resolvedLocations = locations.GroupBy(location => location.MatchLocationId).Select(copiesOfLocation =>
                {
                    var resolvedLocation = copiesOfLocation.First();
                    resolvedLocation.Teams = copiesOfLocation.Select(location => location.Teams.SingleOrDefault()).OfType<Team>().Distinct(new TeamEqualityComparer()).ToList();
                    return resolvedLocation;
                }).ToList();

                return resolvedLocations;
            }
        }

        private static (string filteredSql, Dictionary<string, object> parameters) BuildMatchLocationQuery(MatchLocationFilter matchLocationQuery, string sql, string[] currentJoins)
        {
            var join = new List<string>();
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(matchLocationQuery.Query))
            {
                where.Add(@"(ml.SecondaryAddressableObjectName LIKE @Query OR 
                            ml.PrimaryAddressableObjectName LIKE @Query OR 
                            ml.Locality LIKE @Query OR
                            ml.Town LIKE @Query)");
                parameters.Add("@Query", $"%{matchLocationQuery.Query}%");
            }

            if (matchLocationQuery.ExcludeMatchLocationIds?.Count > 0)
            {
                where.Add("ml.MatchLocationId NOT IN @ExcludeMatchLocationIds");
                parameters.Add("@ExcludeMatchLocationIds", matchLocationQuery.ExcludeMatchLocationIds.Select(x => x.ToString()));
            }

            if (matchLocationQuery.HasActiveTeams.HasValue)
            {
                if (matchLocationQuery.HasActiveTeams.Value)
                {
                    if (!currentJoins.Contains(Tables.TeamMatchLocation))
                    {
                        join.Add($"LEFT JOIN {Tables.TeamMatchLocation} AS tml ON ml.MatchLocationId = tml.MatchLocationId");
                    }
                    if (!currentJoins.Contains(Tables.Team))
                    {
                        join.Add($"LEFT JOIN {Tables.Team} AS t ON tml.TeamId = t.TeamId");
                    }
                    if (!currentJoins.Contains(Tables.TeamVersion))
                    {
                        join.Add($"LEFT JOIN {Tables.TeamVersion} AS tv ON t.TeamId = tv.TeamId");
                    }

                    where.Add("tv.TeamVersionId IS NOT NULL AND tv.UntilDate IS NULL");
                }
                else throw new NotImplementedException();
            }

            if (matchLocationQuery.TeamTypes.Count > 0)
            {
                if (!currentJoins.Contains(Tables.TeamMatchLocation))
                {
                    join.Add($"LEFT JOIN {Tables.TeamMatchLocation} AS tml ON ml.MatchLocationId = tml.MatchLocationId");
                }
                if (!currentJoins.Contains(Tables.Team))
                {
                    join.Add($"LEFT JOIN {Tables.Team} AS t ON tml.TeamId = t.TeamId");
                }

                where.Add("t.TeamType IN @TeamTypes");
                parameters.Add("@TeamTypes", matchLocationQuery.TeamTypes.Select(x => x.ToString()));
            }

            if (matchLocationQuery.SeasonIds.Count > 0)
            {
                if (!currentJoins.Contains(Tables.TeamMatchLocation))
                {
                    join.Add($"LEFT JOIN {Tables.TeamMatchLocation} AS tml ON ml.MatchLocationId = tml.MatchLocationId");
                }
                if (!currentJoins.Contains(Tables.SeasonTeam))
                {
                    join.Add($"LEFT JOIN {Tables.SeasonTeam} AS st ON tml.TeamId = st.TeamId");
                }

                where.Add("st.SeasonId IN @SeasonIds");
                parameters.Add("@SeasonIds", matchLocationQuery.SeasonIds.Select(x => x.ToString()));
            }

            sql = sql.Replace("<<JOIN>>", join.Count > 0 ? string.Join(" ", join) : string.Empty)
                 .Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "WHERE 1=1"); // Ensure there's always a WHERE clause we can append to

            return (sql, parameters);
        }
    }
}
