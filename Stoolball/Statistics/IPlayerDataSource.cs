using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    /// <summary>
    /// Get stoolball player data from a data source
    /// </summary>
    public interface IPlayerDataSource
    {
        /// <summary>
        /// Gets a list of players and their identities
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no players are found.</returns>
        Task<List<Player>> ReadPlayers(PlayerFilter filter);

        /// <summary>
        /// Gets a list of players and their identities
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no players are found.</returns>
        Task<List<Player>> ReadPlayers(PlayerFilter filter, IDbConnection connection);

        /// <summary>
        /// Gets a list of player identities based on a query
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no player identities are found.</returns>
        Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerFilter filter);

        /// <summary>
        /// Read a single player by their route, with statistics about each player identity
        /// </summary>
        /// <param name="route"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Player?> ReadPlayerByRoute(string route, StatisticsFilter? filter = null);

        /// <summary>
        /// Read a single player by the GUID of the member it is linked to
        /// </summary>
        /// <returns>A matching player, or <c>null</c> if no player is linked</returns>
        Task<Player> ReadPlayerByMemberKey(Guid key);
    }
}