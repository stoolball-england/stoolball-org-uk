using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Competitions
{
    /// <summary>
    /// Get stoolball competition data from a data source
    /// </summary>
    public interface ICompetitionDataSource
    {
        /// <summary>
        /// Gets the number of competitions that match a query
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalCompetitions(CompetitionFilter filter);

        /// <summary>
        /// Gets a list of competitions based on a query
        /// </summary>
        /// <returns>A list of <see cref="Competition"/> objects. An empty list if no competitions are found.</returns>
        Task<List<Competition>> ReadCompetitions(CompetitionFilter filter);

        /// <summary>
        /// Gets a single stoolball competition based on its route
        /// </summary>
        /// <param name="route">/competitions/example-competition</param>
        /// <returns>A matching <see cref="Competition"/> or <c>null</c> if not found</returns>
        Task<Competition> ReadCompetitionByRoute(string route);
    }
}