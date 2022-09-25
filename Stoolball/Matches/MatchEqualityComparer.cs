using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class MatchEqualityComparer : EqualityComparer<Match>
    {
        public override bool Equals(Match x, Match y)
        {
            if (x?.MatchId != null && y?.MatchId != null)
            {
                return x.MatchId.Value == y.MatchId.Value;
            }
            else if (!string.IsNullOrEmpty(x?.MatchRoute) && !string.IsNullOrEmpty(y?.MatchRoute))
            {
                return x.MatchRoute.Equals(y.MatchRoute);
            }
            return false;
        }

        public override int GetHashCode(Match obj)
        {
            return base.GetHashCode();
        }
    }
}
