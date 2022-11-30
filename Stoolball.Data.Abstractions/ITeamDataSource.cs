using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Teams;

namespace Stoolball.Data.Abstractions
{
    /// <summary>
    /// Get stoolball team data from a data source
    /// </summary>
    public interface ITeamDataSource
    {
        /// <summary>
        /// Gets the number of teams that match a query
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalTeams(TeamFilter teamQuery);

        /// <summary>
        /// Gets a list of teams based on a query
        /// </summary>
        /// <returns>A list of <see cref="Team"/> objects. An empty list if no teams are found.</returns>
        Task<List<Team>> ReadTeams(TeamFilter teamQuery);

        /// <summary>
        /// Gets a single team based on its route
        /// </summary>
        /// <param name="route">/teams/example-team</param>
        /// <param name="includeRelated"><c>true</c> to include the club, match locations and seasons; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="Team"/> or <c>null</c> if not found</returns>
        Task<Team?> ReadTeamByRoute(string route, bool includeRelated = false);
    }
}