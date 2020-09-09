using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IBowlingScorecardComparer
    {
        BowlingScorecardComparison CompareScorecards(IEnumerable<Over> before, IEnumerable<Over> after);
    }
}