using Stoolball.Matches;
using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class TeamInMatchEqualityComparer : EqualityComparer<TeamInMatch>
    {
        public override bool Equals(TeamInMatch x, TeamInMatch y)
        {
            return x?.Team.TeamId == y?.Team.TeamId && x?.TeamRole == y?.TeamRole;
        }

        public override int GetHashCode(TeamInMatch obj)
        {
            return base.GetHashCode();
        }
    }
}

