using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IOversHelper
    {
        OverSet? OverSetForOver(IEnumerable<OverSet> overSets, int overNumber);
        int OversToBallsBowled(decimal overs);
    }
}