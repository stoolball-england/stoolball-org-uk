using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.MatchLocations
{
    /// <summary>
    /// Get stoolball match location data from a data source
    /// </summary>
    public interface IMatchLocationDataSource
    {
        /// <summary>
        /// Gets the number of match locations that match a query
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalMatchLocations(MatchLocationQuery matchLocationQuery);

        /// <summary>
        /// Gets a list of match locations based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchLocation"/> objects. An empty list if no match locations are found.</returns>
        Task<List<MatchLocation>> ReadMatchLocations(MatchLocationQuery matchLocationQuery);

        /// <summary>
        /// Gets a single match location based on its route
        /// </summary>
        /// <param name="route">/locations/example-location</param>
        /// <param name="includeRelated"><c>true</c> to include the teams based at the selected location; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="MatchLocation"/> or <c>null</c> if not found</returns>
        Task<MatchLocation> ReadMatchLocationByRoute(string route, bool includeRelated = false);
    }
}