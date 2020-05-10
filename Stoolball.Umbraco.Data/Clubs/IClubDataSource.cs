using Stoolball.Clubs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Clubs
{
    /// <summary>
    /// Get stoolball club data from a data source
    /// </summary>
    public interface IClubDataSource
    {
        /// <summary>
        /// Gets a list of clubs based on a query
        /// </summary>
        /// <returns>A list of <see cref="Club"/> objects. An empty list if no clubs are found.</returns>
        Task<List<Club>> ReadClubListings(ClubQuery clubQuery);

        /// <summary>
        /// Gets a single stoolball club based on its route
        /// </summary>
        /// <param name="route">/clubs/example-club</param>
        /// <returns>A matching <see cref="Club"/> or <c>null</c> if not found</returns>
        Task<Club> ReadClubByRoute(string route);
    }
}