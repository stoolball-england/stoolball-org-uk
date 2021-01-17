using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    /// <summary>
    /// Calculate bowling figures (overs, maidens, runs, wickets) from data in a <see cref="MatchInnings"/>.
    /// </summary>
    public class BowlingFiguresCalculator : IBowlingFiguresCalculator
    {
        /// <summary>
        /// Calculate bowling figures (overs, maidens, runs, wickets) from data in a <see cref="MatchInnings"/>.
        /// </summary>
        public IList<BowlingFigures> CalculateBowlingFigures(MatchInnings innings)
        {
            if (innings is null)
            {
                throw new System.ArgumentNullException(nameof(innings));
            }

            // Get all bowlers who bowled, in the order they bowled
            var bowlers = innings.OversBowled.OrderBy(x => x.OverNumber).Select(x => x.PlayerIdentity.PlayerIdentityId).Distinct().ToList();

            // Add unexpected bowlers who are recorded as taking wickets but not bowling
            bowlers.AddRange(innings.PlayerInnings
                .Where(x => x.DismissalType.HasValue
                            && x.Bowler != null
                            && !bowlers.Contains(x.Bowler.PlayerIdentityId)
                            && StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(x.DismissalType.Value))
                .Select(x => x.Bowler.PlayerIdentityId));

            // For each bowler, add their figures
            var bowlingFigures = new List<BowlingFigures>();

            foreach (var bowler in bowlers)
            {
                double? overs = null;
                int? maidens = null, runsConceded = null;

                var oversByThisBowler = innings.OversBowled.Where(x => x.PlayerIdentity.PlayerIdentityId == bowler);

                if (oversByThisBowler.Any())
                {
                    // Work out how many balls bowled (if any were recorded) and from that how many overs.
                    // Note that we're working using base 10 maths to get a figure that people would use for overs, but that figure actually represents a base 8 (or base BALLS_PER_OVER) number.
                    // That's fine until we start calculating with it. For now, we just want the numbers people would work out themselves from bowling data.
                    var ballsBowled = oversByThisBowler.Where(x => x.BallsBowled.HasValue).Sum(x => x.BallsBowled);

                    if (ballsBowled.HasValue)
                    {
                        var completedOvers = Math.Floor((double)(ballsBowled.Value / StatisticsConstants.BALLS_PER_OVER));
                        var additionalBalls = Math.Floor((double)(ballsBowled.Value % StatisticsConstants.BALLS_PER_OVER));
                        overs = completedOvers + (additionalBalls * .1);
                    }

                    // Only completed overs count as maidens
                    maidens = oversByThisBowler.Count(x => x.BallsBowled.HasValue && x.BallsBowled >= StatisticsConstants.BALLS_PER_OVER && x.RunsConceded == 0);

                    runsConceded = oversByThisBowler.Where(x => x.RunsConceded.HasValue).Sum(x => x.RunsConceded);
                }

                // Wickets taken comes from the batting data
                var wickets = innings.PlayerInnings.Count(x => x.DismissalType.HasValue
                                                               && x.Bowler != null
                                                               && StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(x.DismissalType.Value)
                                                               && x.Bowler.PlayerIdentityId == bowler);

                bowlingFigures.Add(new BowlingFigures
                {
                    Bowler = new PlayerIdentity
                    {
                        PlayerIdentityId = bowler
                    },
                    Overs = overs,
                    Maidens = maidens,
                    RunsConceded = runsConceded,
                    Wickets = wickets
                });
            }

            return bowlingFigures;
        }
    }
}
