using Stoolball.Matches;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Get stoolball match data from a data source
    /// </summary>
    public interface IMatchDataSource
    {
        /// <summary>
        /// Gets the number of matches and tournaments that match a query
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalMatches(MatchQuery matchQuery);

        /// <summary>
        /// Gets a list of matches and tournaments based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchListing"/> objects. An empty list if no matches or tournaments are found.</returns>
        Task<List<MatchListing>> ReadMatchListings(MatchQuery matchQuery);

        /// <summary>
        /// Gets a single match based on its route
        /// </summary>
        /// <param name="route">/matches/example-match</param>
        /// <returns>A matching <see cref="Match"/> or <c>null</c> if not found</returns>
        Task<Match> ReadMatchByRoute(string route);
    }
}