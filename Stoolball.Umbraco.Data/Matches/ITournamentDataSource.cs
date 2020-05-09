using Stoolball.Matches;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Get stoolball tournament data from a data source
    /// </summary>
    public interface ITournamentDataSource
    {
        /// <summary>
        /// Gets a single tournament based on its route
        /// </summary>
        /// <param name="route">/tournaments/example-tournament</param>
        /// <returns>A matching <see cref="Tournament"/> or <c>null</c> if not found</returns>
        Task<Tournament> ReadTournamentByRoute(string route);
    }
}