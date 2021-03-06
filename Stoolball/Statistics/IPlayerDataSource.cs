﻿using System.Collections.Generic;
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
        /// Read a single player by their route
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        Task<Player> ReadPlayerByRoute(string route);
    }
}