using System.Collections.Generic;

namespace Stoolball.MatchLocations
{
    public class MatchLocationEqualityComparer : EqualityComparer<MatchLocation>
    {
        public override bool Equals(MatchLocation x, MatchLocation y)
        {
            return x?.MatchLocationId == y?.MatchLocationId;
        }

        public override int GetHashCode(MatchLocation obj)
        {
            return base.GetHashCode();
        }
    }
}
