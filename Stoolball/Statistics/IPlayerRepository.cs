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
        /// Sets the MemberKey on a player to the supplied MemberKey, or moves the identities from <c>player</c> to an existing player if one is already linked.
        /// </summary>
        /// <param name="player">The player to update</param>
        /// <param name="memberKey">The MemberKey to associate to the member, and the key of the member to update</param>
        /// <param name="memberName">The name of the member making the update</param>
        /// <returns>The combined player, with updated <c>PlayerId</c>, <c>PlayerRoute</c> and <c>MemberKey</c> if appropriate.</returns>
        Task<Player> LinkPlayerToMemberAccount(Player player, Guid memberKey, string memberName);

        /// <summary>
        /// Removes the player identity from its player, creating a new player not linked to the member. Or for the last player identity linked to a member, removes the MemberKey from the player.
        /// </summary>
        /// <param name="playerIdentity">The player identity to unlink</param>
        /// <param name="memberKey">The member to unlink the player identity from</param>
        /// <param name="membername">The name of the member making the update</param>
        /// <returns></returns>
        Task UnlinkPlayerIdentityFromMemberAccount(PlayerIdentity playerIdentity, Guid memberKey, string memberName);

        /// <summary>
        /// <see cref="LinkPlayerToMemberAccount"/> and <see cref="UnlinkPlayerIdentityFromMemberAccount"/> both leave updates to statistics and cleaning up unused resources incomplete, to be done asynchronously. This completes the work.
        /// </summary>
        /// <returns></returns>
        Task ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts();
    }
}