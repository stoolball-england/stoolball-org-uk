using Stoolball.Clubs;
using Stoolball.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;

namespace Stoolball.Umbraco.Data.Clubs
{
    /// <summary>
    /// Gets stoolball club data from the Umbraco database
    /// </summary>
    public class SqlServerClubDataSource : IClubDataSource
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly ILogger _logger;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerClubDataSource(IScopeProvider scopeProvider, ILogger logger, IRouteNormaliser routeNormaliser)
        {
            _scopeProvider = scopeProvider ?? throw new System.ArgumentNullException(nameof(scopeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single stoolball club based on its route
        /// </summary>
        /// <param name="route">clubs/example-club</param>
        /// <returns>A matching <see cref="Club"/> or <c>null</c> if not found</returns>
        public Task<Club> ReadClubByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "clubs");

                using (var scope = _scopeProvider.CreateScope())
                {
                    var club = scope.Database.FetchOneToMany<Club>(x => x.Teams,
                        $@"SELECT c.ClubId, cn.ClubName, c.HowManyPlayers, c.PlaysOutdoors, c.PlaysIndoors,
                           c.Twitter, c.Facebook, c.Instagram, c.YouTube, c.Website, c.ClubMark, c.ClubRoute,
                           tn.TeamName, t.TeamRoute
                           FROM {Constants.Tables.Club} AS c 
                           INNER JOIN {Constants.Tables.ClubName} AS cn ON c.ClubId = cn.ClubId
                           LEFT JOIN {Constants.Tables.Team} AS t ON c.ClubId = t.ClubId
                           LEFT JOIN {Constants.Tables.TeamName} AS tn ON t.TeamId = tn.TeamId
                           WHERE LOWER(c.ClubRoute) = @0 AND cn.UntilDate IS NULL", normalisedRoute).FirstOrDefault();

                    scope.Complete();
                    return Task.FromResult(club);
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
