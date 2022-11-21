using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    internal class TestDataQueryHelper
    {
        internal int TotalMatchesForPlayer(Player player, IList<Match> matches, Func<Match, bool> matchFilter, Func<MatchInnings, bool> bowlingInningsFilter, Func<MatchInnings, bool> battingInningsFilter, Func<MatchAward, bool> awardsFilter)
        {
            return (int)matches.Where(matchFilter)
                               .Count(m => m.MatchInnings.Where(battingInningsFilter).Any(mi => mi.PlayerInnings.Any(pi => pi.Batter?.Player?.PlayerId == player.PlayerId))
                                    || m.MatchInnings.Where(bowlingInningsFilter).Any(mi =>
                                        mi.PlayerInnings.Any(pi => pi.DismissedBy?.Player?.PlayerId == player.PlayerId || pi.Bowler?.Player?.PlayerId == player.PlayerId) ||
                                        mi.OversBowled.Any(o => o.Bowler?.Player?.PlayerId == player.PlayerId) ||
                                        mi.BowlingFigures.Any(bf => bf.Bowler?.Player?.PlayerId == player.PlayerId)
                                    ) || m.Awards.Where(awardsFilter).Any(aw => aw.PlayerIdentity?.Player?.PlayerId == player.PlayerId));
        }
    }
}
