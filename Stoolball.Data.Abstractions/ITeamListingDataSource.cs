using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Teams;

namespace Stoolball.Data.Abstractions
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
        Task<int> ReadTotalTeams(TeamListingFilter filter);

        /// <summary>
        /// Gets a list of clubs and teams based on a query
        /// </summary>
        /// <returns>A list of <see cref="TeamListing"/> objects. An empty list if no clubs or teams are found.</returns>
        Task<List<TeamListing>> ReadTeamListings(TeamListingFilter filter);
    }
}