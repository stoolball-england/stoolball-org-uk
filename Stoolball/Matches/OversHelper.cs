using System;
using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Matches
{
    public class OversHelper : IOversHelper
    {
        public OverSet? OverSetForOver(IEnumerable<OverSet> overSets, int overNumber)
        {
            if (overSets is null)
            {
                return null;
            }

            var over = 1;
            foreach (var set in overSets)
            {
                var totalOvers = over + set.Overs;
                do
                {
                    if (over == overNumber)
                    {
                        return set;
                    }
                    over++;
                } while (over < totalOvers);
            }

            return null;
        }

        /// <summary>
        /// Converts a number of 8-ball overs into a number of balls bowled
        /// </summary>
        public int OversToBallsBowled(decimal overs)
        {
            // Work out how many balls were bowled - have to assume 8 ball overs, even though on rare occasions that may not be the case
            var completedOvers = (int)Math.Floor(overs);
            var additionalBalls = (int)((overs - completedOvers) * 10); // *10 changes 0.6 overs into 6 balls, for example
            return (completedOvers * StatisticsConstants.BALLS_PER_OVER) + additionalBalls;
        }

        /// <summary>
        /// Converts a number of balls bowled into 8-ball overs
        /// </summary>
        public decimal BallsBowledToOvers(int ballsBowled)
        {
            return (ballsBowled / StatisticsConstants.BALLS_PER_OVER) + (decimal)(ballsBowled % StatisticsConstants.BALLS_PER_OVER) / 10; // divide by 10 changes 6 balls into 0.6 overs, for example
        }
    }
}
