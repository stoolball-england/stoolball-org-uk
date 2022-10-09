using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class OverSetEqualityComparer : EqualityComparer<OverSet>
    {
        public override bool Equals(OverSet? x, OverSet? y)
        {
            return x?.OverSetId == y?.OverSetId;
        }

        public override int GetHashCode(OverSet obj)
        {
            return base.GetHashCode();
        }
    }
}
