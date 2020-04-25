using Stoolball.MatchLocations;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.MatchLocations
{
    /// <summary>
    /// Get stoolball match location data from a data source
    /// </summary>
    public interface IMatchLocationDataSource
    {
        /// <summary>
        /// Gets a single match location based on its route
        /// </summary>
        /// <param name="route">/locations/example-location</param>
        /// <param name="includeRelated"><c>true</c> to include the teams based at the selected location; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="MatchLocation"/> or <c>null</c> if not found</returns>
        Task<MatchLocation> ReadMatchLocationByRoute(string route, bool includeRelated = false);
    }
}