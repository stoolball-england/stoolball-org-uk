using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Matches
{
    /// <summary>
    /// Get stoolball match data from a data source
    /// </summary>
    public interface IMatchListingDataSource
    {
        /// <summary>
        /// Gets the number of matches and tournaments that match a query
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalMatches(MatchFilter matchQuery);

        /// <summary>
        /// Gets a list of matches and tournaments based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchListing"/> objects. An empty list if no matches or tournaments are found.</returns>
        Task<List<MatchListing>> ReadMatchListings(MatchFilter matchQuery, MatchSortOrder sortOrder);
    }
}