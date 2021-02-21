using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class PlayerIdentityFinder : IPlayerIdentityFinder
    {
        public IEnumerable<PlayerIdentity> PlayerIdentitiesInMatch(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            var comparer = new PlayerIdentityEqualityComparer();

            return match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => !string.IsNullOrEmpty(pi.Batter.PlayerIdentityName)).Select(pi => pi.Batter))
                .Union(match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null && !string.IsNullOrEmpty(pi.DismissedBy.PlayerIdentityName)).Select(pi => pi.DismissedBy)), comparer)
                .Union(match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null && !string.IsNullOrEmpty(pi.Bowler.PlayerIdentityName)).Select(pi => pi.Bowler)), comparer)
                .Union(match.MatchInnings.SelectMany(i => i.OversBowled.Where(pi => !string.IsNullOrEmpty(pi.Bowler.PlayerIdentityName)).Select(pi => pi.Bowler)), comparer)
                .Union(match.MatchInnings.SelectMany(i => i.BowlingFigures.Where(pi => !string.IsNullOrEmpty(pi.Bowler.PlayerIdentityName)).Select(pi => pi.Bowler)), comparer)
                .Union(match.Awards.Where(award => !string.IsNullOrEmpty(award.PlayerIdentity?.PlayerIdentityName)).Select(x => x.PlayerIdentity)).Distinct(comparer);
        }
    }
}
