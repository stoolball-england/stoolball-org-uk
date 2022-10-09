using System.Collections.Generic;

namespace Stoolball.Teams
{
    public class TeamEqualityComparer : EqualityComparer<Team>
    {
        public override bool Equals(Team? x, Team? y)
        {
            if (x?.TeamId != null && y?.TeamId != null)
            {
                return x.TeamId.Value == y.TeamId.Value;
            }
            else if (!string.IsNullOrEmpty(x?.TeamRoute) && !string.IsNullOrEmpty(y?.TeamRoute))
            {
                return x.TeamRoute.Equals(y.TeamRoute);
            }
            return false;
        }

        public override int GetHashCode(Team obj)
        {
            return base.GetHashCode();
        }
    }
}
