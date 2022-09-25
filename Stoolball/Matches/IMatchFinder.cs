using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IMatchFinder
    {
        /// <summary>
        /// Find the matches within a given set of matches where the given player identity features in any role.
        /// </summary>
        /// <param name="matches">The set of matches to search within</param>
        /// <param name="playerIdentityId">The player identity to search for</param>
        /// <returns>A set of matches</returns>
        /// <exception cref="ArgumentNullException">Thrown if <c>matches</c> is null</exception>
        IEnumerable<Match> MatchesPlayedByPlayerIdentity(IEnumerable<Match> matches, Guid playerIdentityId);

        /// <summary>
        /// Find the matches within a given set of matches where the given player features in any role.
        /// </summary>
        /// <param name="matches">The set of matches to search within</param>
        /// <param name="playerId">The player to search for</param>
        /// <returns>A set of matches</returns>
        /// <exception cref="ArgumentNullException">Thrown if <c>matches</c> is null</exception>
        IEnumerable<Match> MatchesPlayedByPlayer(IEnumerable<Match> matches, Guid playerId);
    }
}