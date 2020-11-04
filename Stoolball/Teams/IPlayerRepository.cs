using System;
using System.Threading.Tasks;

namespace Stoolball.Teams
{
    public interface IPlayerRepository
    {
        /// <summary>
        /// Finds an existing player identity or creates it if it is not found
        /// </summary>
        /// <returns>The <see cref="PlayerIdentity.PlayerIdentityId"/> of the created or matched player identity</returns>
        Task<Guid> CreateOrMatchPlayerIdentity(PlayerIdentity playerIdentity, Guid memberKey, string memberName);
    }
}