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

            return match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.Batter))
                .Union(match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy)))
                .Union(match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler)))
                .Union(match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.Bowler)))
                .Union(match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler)))
                .Union(match.Awards.Select(x => x.PlayerIdentity)).Distinct(new PlayerIdentityEqualityComparer());
        }
    }
}
