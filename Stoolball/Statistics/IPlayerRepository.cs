using System;
using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IPlayerRepository
    {
        /// <summary>
        /// Finds an existing player identity or creates it if it is not found
        /// </summary>
        /// <returns>The <see cref="PlayerIdentity"/> of the created or matched player identity</returns>
        Task<PlayerIdentity> CreateOrMatchPlayerIdentity(PlayerIdentity playerIdentity, Guid memberKey, string memberName, IDbTransaction transaction);

        /// <summary>
        /// Sets the MemberKey on a player to the supplied MemberKey
        /// </summary>
        /// <param name="player">The player to update</param>
        /// <param name="memberKey">The MemberKey to associate to the member, and the key of the member to update</param>
        /// <param name="memberName">The name of the member making the update</param>
        /// <returns>The combined player, with updated <c>PlayerId</c>, <c>PlayerRoute</c> and <c>MemberKey</c> if appropriate.</returns>
        Task<Player> LinkPlayerToMemberAccount(Player player, Guid memberKey, string memberName);
    }
}