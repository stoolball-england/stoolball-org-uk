using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    /// <summary>
    /// Calculate bowling figures (overs, maidens, runs, wickets) from data in a <see cref="MatchInnings"/>.
    /// </summary>
    public class BowlingFiguresCalculator : IBowlingFiguresCalculator
    {
        private readonly IOversHelper _oversHelper;

        public BowlingFiguresCalculator(IOversHelper oversHelper)
        {
            _oversHelper = oversHelper ?? throw new ArgumentNullException(nameof(oversHelper));
        }

        /// <summary>
        /// Calculate bowling figures (overs, maidens, runs, wickets) from data in a <see cref="MatchInnings"/>.
        /// </summary>
        public List<BowlingFigures> CalculateBowlingFigures(MatchInnings innings)
        {
            if (innings is null)
            {
                throw new System.ArgumentNullException(nameof(innings));
            }

            // Get all bowlers who bowled, in the order they bowled
            var bowlerNames = innings.OversBowled.OrderBy(x => x.OverNumber).Select(x => x.Bowler.ComparableName()).Distinct().ToList();
            var bowlers = bowlerNames.Select(name => innings.OversBowled.First(over => over.Bowler.ComparableName() == name).Bowler).ToList();

            // Add unexpected bowlers who are recorded as taking wickets but not bowling
            foreach (var bowlerOnBattingCard in innings.PlayerInnings
                                                .Where(x => x.DismissalType.HasValue
                                                            && x.Bowler != null
                                                            && !bowlerNames.Contains(x.Bowler.ComparableName())
                                                            && StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(x.DismissalType.Value))
                                                .Select(x => x.Bowler).Distinct(new PlayerIdentityEqualityComparer()))
            {
                bowlers.Add(bowlerOnBattingCard);
            }

            // For each bowler, add their figures
            var bowlingFigures = new List<BowlingFigures>();

            foreach (var bowler in bowlers)
            {
                decimal? overs = null;
                int? maidens = null, runsConceded = null;

                var oversByThisBowler = innings.OversBowled.Where(x => x.Bowler.ComparableName() == bowler.ComparableName());

                if (oversByThisBowler.Any())
                {
                    // Work out how many balls bowled (if any were recorded) and from that how many overs.
                    // Note that we're working using base 10 maths to get a figure that people would use for overs, but that figure actually represents a base 8 (or base BALLS_PER_OVER) number.
                    // That's fine until we start calculating with it. For now, we just want the numbers people would work out themselves from bowling data.
                    var ballsBowled = oversByThisBowler.Where(x => x.BallsBowled.HasValue).Sum(x => x.BallsBowled);

                    if (ballsBowled.HasValue)
                    {
                        var completedOvers = Math.Floor((decimal)(ballsBowled.Value / StatisticsConstants.BALLS_PER_OVER));
                        var additionalBalls = Math.Floor((decimal)(ballsBowled.Value % StatisticsConstants.BALLS_PER_OVER));
                        overs = completedOvers + (additionalBalls * (decimal).1);
                    }

                    // Only completed overs count as maidens
                    maidens = oversByThisBowler.Count(x => x.BallsBowled.HasValue && x.BallsBowled >= StatisticsConstants.BALLS_PER_OVER && x.RunsConceded == 0);

                    runsConceded = oversByThisBowler.Where(x => x.RunsConceded.HasValue).Sum(x => x.RunsConceded);
                }

                // Wickets taken comes from the batting data
                var wickets = innings.PlayerInnings.Count(x => x.DismissalType.HasValue
                                                               && x.Bowler != null
                                                               && StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(x.DismissalType.Value)
                                                               && (x.Bowler.ComparableName() == bowler.ComparableName()));

                bowlingFigures.Add(new BowlingFigures
                {
                    BowlingFiguresId = Guid.NewGuid(),
                    Bowler = bowler,
                    Overs = overs,
                    Maidens = maidens,
                    RunsConceded = runsConceded,
                    Wickets = wickets
                });
            }

            return bowlingFigures;
        }

        /// <summary>
        /// Gets the number of runs conceded per wicket taken for a bowling performance
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingAverage(BowlingFigures bowlingFigures)
        {
            if (bowlingFigures is null)
            {
                throw new ArgumentNullException(nameof(bowlingFigures));
            }

            if (bowlingFigures.Wickets > 0 && bowlingFigures.RunsConceded.HasValue)
            {
                return Math.Round((decimal)bowlingFigures.RunsConceded / bowlingFigures.Wickets, 2);
            }

            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the average number of runs conceded per over
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingEconomy(BowlingFigures bowlingFigures)
        {
            if (bowlingFigures is null)
            {
                throw new ArgumentNullException(nameof(bowlingFigures));
            }

            if (bowlingFigures.Overs > 0 && bowlingFigures.RunsConceded.HasValue)
            {
                var oversInDecimal = ((decimal)_oversHelper.OversToBallsBowled(bowlingFigures.Overs.Value)) / StatisticsConstants.BALLS_PER_OVER;
                return Math.Round((decimal)bowlingFigures.RunsConceded / oversInDecimal, 2);
            }

            else return null;
        }

        /// <summary>
        /// Gets the number of balls bowled per wicket
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingStrikeRate(BowlingFigures bowlingFigures)
        {
            if (bowlingFigures is null)
            {
                throw new ArgumentNullException(nameof(bowlingFigures));
            }

            if (bowlingFigures.Overs > 0 && bowlingFigures.Wickets > 0)
            {
                return Math.Round((decimal)_oversHelper.OversToBallsBowled(bowlingFigures.Overs.Value) / bowlingFigures.Wickets, 2);
            }

            else
            {
                return null;
            }
        }
    }
}
