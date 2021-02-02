using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Matches
{
    public class OverSetScaffolder : IOverSetScaffolder
    {
        public void ScaffoldOverSets(List<OverSet> overSets, int defaultOversPerMatch, List<Over> oversBowled)
        {
            if (overSets is null)
            {
                throw new ArgumentNullException(nameof(overSets));
            }

            if (oversBowled is null)
            {
                throw new ArgumentNullException(nameof(oversBowled));
            }

            // Assume the existing over sets are correct - if balls bowled is different for an over, it's more likely 
            // the umpire made a mistake and it was recorded than it is that the format of the innings was changed.

            // What if there weren't any over sets? Create a default one.
            if (!overSets.Any())
            {
                overSets.Add(new OverSet { Overs = oversBowled.Count > defaultOversPerMatch ? oversBowled.Count : defaultOversPerMatch, BallsPerOver = oversBowled.FirstOrDefault()?.BallsBowled ?? 8 });
            }

            // What if there were more overs than the over sets expected?
            // Assume extra overs are the same format as the last known over. Again, if there are higher numbers it's more likely 
            // an umpiring error than a change in the format of the innings.
            if (overSets.Sum(x => x.Overs) < oversBowled.Count)
            {
                overSets[overSets.Count - 1].Overs += (oversBowled.Count - overSets.Sum(x => x.Overs));
            }
        }
    }
}
