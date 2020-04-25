using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class MatchListingEqualityComparer : EqualityComparer<MatchListing>
    {
        public override bool Equals(MatchListing x, MatchListing y)
        {
            return x?.MatchRoute == y?.MatchRoute;
        }

        public override int GetHashCode(MatchListing obj)
        {
            return base.GetHashCode();
        }
    }
}
