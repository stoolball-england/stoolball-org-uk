using System;
using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Matches
{
    public class OversHelper : IOversHelper
    {
        public OverSet OverSetForOver(IEnumerable<OverSet> overSets, int overNumber)
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

        public int OversToBallsBowled(decimal overs)
        {
            var completedOvers = (int)Math.Floor(overs);
            var additionalBalls = (int)((overs - completedOvers) * 10);
            return (completedOvers * StatisticsConstants.BALLS_PER_OVER) + additionalBalls;
        }
    }
}
