using System.Collections.Generic;

namespace Stoolball.Teams
{
    public class TeamEqualityComparer : EqualityComparer<Team>
    {
        public override bool Equals(Team x, Team y)
        {
            return x?.TeamId == y?.TeamId;
        }

        public override int GetHashCode(Team obj)
        {
            return base.GetHashCode();
        }
    }
}
