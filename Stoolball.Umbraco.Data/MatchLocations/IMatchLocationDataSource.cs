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
        /// <returns>A matching <see cref="MatchLocation"/> or <c>null</c> if not found</returns>
        Task<MatchLocation> ReadMatchLocationByRoute(string route);
    }
}