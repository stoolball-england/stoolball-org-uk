using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Teams
{
    /// <summary>
    /// Get stoolball player data from a data source
    /// </summary>
    public interface IPlayerDataSource
    {
        /// <summary>
        /// Gets a list of player identities based on a query
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no player identities are found.</returns>
        Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerIdentityQuery playerQuery);

        /// <summary>
        /// Read a single player by their route
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        Task<Player> ReadPlayerByRoute(string route);
    }
}