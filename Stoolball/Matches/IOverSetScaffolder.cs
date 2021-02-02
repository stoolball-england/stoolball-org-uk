using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IOverSetScaffolder
    {
        void ScaffoldOverSets(List<OverSet> overSets, int defaultOversPerMatch, List<Over> oversBowled);
    }
}