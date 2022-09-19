using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Matches
{
    public class MatchFinder : IMatchFinder
    {
        /// <summary>
        /// Find the matches within a given set of matches where the given player identity features in any role.
        /// </summary>
        /// <param name="matches">The set of matches to search within</param>
        /// <param name="playerIdentityId">The player identity to search for</param>
        /// <returns>A set of matches</returns>
        /// <exception cref="ArgumentNullException">Thrown if <c>matches</c> is null</exception>
        public IEnumerable<Match> MatchesPlayedByPlayerIdentity(IEnumerable<Match> matches, Guid playerIdentityId)
        {
            if (matches is null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            return matches.Where(match => match.MatchInnings.Any(mi =>
                                    mi.OversBowled.Any(o => o.Bowler.PlayerIdentityId == playerIdentityId) ||
                                    mi.PlayerInnings.Any(pi => pi.Batter.PlayerIdentityId == playerIdentityId) ||
                                    mi.PlayerInnings.Any(pi => pi.DismissedBy?.PlayerIdentityId == playerIdentityId) ||
                                    mi.PlayerInnings.Any(pi => pi.Bowler?.PlayerIdentityId == playerIdentityId)) ||
                                    match.Awards.Any(aw => aw.PlayerIdentity?.PlayerIdentityId == playerIdentityId));
        }
    }
}
