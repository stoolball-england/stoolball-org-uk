using Stoolball.Teams;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Teams
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
        Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerIdentityQuery teamQuery);
    }
}