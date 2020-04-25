using Dapper;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;

namespace Stoolball.Umbraco.Data.MatchLocations
{
    /// <summary>
    /// Gets match location data from the Umbraco database
    /// </summary>
    public class SqlServerMatchLocationDataSource : IMatchLocationDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerMatchLocationDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "locations");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var locations = await connection.QueryAsync<MatchLocation>(
                        $@"SELECT ml.MatchLocationId, ml.MatchLocationRoute,
                            ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.Locality, ml.Town 
                            FROM {Constants.Tables.MatchLocation} AS ml
                            WHERE LOWER(ml.MatchLocationRoute) = @Route",
                        new { Route = normalisedRoute }).ConfigureAwait(false);

                    return locations.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerMatchLocationDataSource), ex);
                throw;
            }
        }

        private async Task<MatchLocation> ReadMatchLocationWithRelatedDataByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "locations");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var locations = await connection.QueryAsync<MatchLocation, Team, MatchLocation>(
                        $@"SELECT ml.MatchLocationId, ml.MatchLocationNotes, ml.MatchLocationRoute,
                            ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.StreetDescription, ml.Locality, ml.Town, ml.AdministrativeArea, ml.Postcode, 
                            ml.Latitude, ml.Longitude, ml.GeoPrecision,
                            t.TeamId, tn.TeamName, t.TeamRoute
                            FROM {Constants.Tables.MatchLocation} AS ml
                            LEFT JOIN {Constants.Tables.TeamMatchLocation} AS tml ON ml.MatchLocationId = tml.MatchLocationId AND tml.UntilDate IS NULL
                            LEFT JOIN {Constants.Tables.Team} AS t ON tml.TeamId = t.TeamId AND t.UntilDate IS NULL AND NOT t.TeamType = '{TeamType.Once}'
                            LEFT JOIN {Constants.Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
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
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerMatchLocationDataSource), ex);
                throw;
            }
        }
    }
}
