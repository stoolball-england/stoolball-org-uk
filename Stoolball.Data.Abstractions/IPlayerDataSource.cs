﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Stoolball.Statistics;

namespace Stoolball.Data.Abstractions
{
    /// <summary>
    /// Get stoolball player data from a data source
    /// </summary>
    public interface IPlayerDataSource
    {
        /// <summary>
        /// Gets a list of players who have played at least one match, and their identities.
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no players are found.</returns>
        Task<List<Player>> ReadPlayers(PlayerFilter filter);

        /// <summary>
        /// Gets a list of players who have played at least one match, and their identities.
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no players are found.</returns>
        Task<List<Player>> ReadPlayers(PlayerFilter filter, IDbConnection connection);

        /// <summary>
        /// Gets a list of player identities who have played at least one match, based on a query.
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no player identities are found.</returns>
        Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerFilter filter);

        /// <summary>
        /// Read a single player by their route, with statistics about each player identity.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Player?> ReadPlayerByRoute(string route, StatisticsFilter? filter = null);

        /// <summary>
        /// Read a single player by the GUID of the member it is linked to.
        /// </summary>
        /// <returns>A matching player, or <c>null</c> if no player is linked</returns>
        Task<Player?> ReadPlayerByMemberKey(Guid key);

        /// <summary>
        /// Read a single player identity by a route including the team and the route segment of the identity.
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        Task<PlayerIdentity?> ReadPlayerIdentityByRoute(string route);
    }
}