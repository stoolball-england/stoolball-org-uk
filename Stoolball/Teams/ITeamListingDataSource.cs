using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Teams
{
    /// <summary>
    /// Get stoolball club and team data from a data source
    /// </summary>
    public interface ITeamListingDataSource
    {
        /// <summary>
        /// Gets the number of clubs and teams that match a query
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalTeams(TeamFilter teamQuery);

        /// <summary>
        /// Gets a list of clubs and teams based on a query
        /// </summary>
        /// <returns>A list of <see cref="TeamListing"/> objects. An empty list if no clubs or teams are found.</returns>
        Task<List<TeamListing>> ReadTeamListings(TeamFilter teamQuery);
    }
}