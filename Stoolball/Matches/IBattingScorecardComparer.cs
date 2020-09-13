using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IBattingScorecardComparer
    {
        BattingScorecardComparison CompareScorecards(IEnumerable<PlayerInnings> before, IEnumerable<PlayerInnings> after);
    }
}