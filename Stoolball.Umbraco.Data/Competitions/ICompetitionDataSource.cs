using Stoolball.Competitions;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Competitions
{
    /// <summary>
    /// Get stoolball competition data from a data source
    /// </summary>
    public interface ICompetitionDataSource
    {
        /// <summary>
        /// Gets a single stoolball competition based on its route
        /// </summary>
        /// <param name="route">/competitions/example-competition</param>
        /// <returns>A matching <see cref="Competition"/> or <c>null</c> if not found</returns>
        Task<Competition> ReadCompetitionByRoute(string route);
    }
}