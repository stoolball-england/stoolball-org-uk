using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Matches
{
    public class MatchFinder : IMatchFinder
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public IEnumerable<Match> MatchesPlayedByPlayer(IEnumerable<Match> matches, Guid playerId)
        {
            if (matches is null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            return matches.Where(match => match.MatchInnings.Any(mi =>
                                    mi.OversBowled.Any(o => o.Bowler.Player.PlayerId == playerId) ||
                                    mi.PlayerInnings.Any(pi => pi.Batter.Player.PlayerId == playerId) ||
                                    mi.PlayerInnings.Any(pi => pi.DismissedBy?.Player.PlayerId == playerId) ||
                                    mi.PlayerInnings.Any(pi => pi.Bowler?.Player.PlayerId == playerId)) ||
                                    match.Awards.Any(aw => aw.PlayerIdentity?.Player.PlayerId == playerId));
        }
    }
}
