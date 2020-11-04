using System.Threading.Tasks;

namespace Stoolball.Matches
{
    /// <summary>
    /// Get stoolball match data from a data source
    /// </summary>
    public interface IMatchDataSource
    {
        /// <summary>
        /// Gets a single match based on its route
        /// </summary>
        /// <param name="route">/matches/example-match</param>
        /// <returns>A matching <see cref="Match"/> or <c>null</c> if not found</returns>
        Task<Match> ReadMatchByRoute(string route);
    }
}