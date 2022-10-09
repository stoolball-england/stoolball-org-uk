using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class TeamInTournamentEqualityComparer : EqualityComparer<TeamInTournament>
    {
        public override bool Equals(TeamInTournament? x, TeamInTournament? y)
        {
            return x?.Team?.TeamId == y?.Team?.TeamId && x?.TeamRole == y?.TeamRole;
        }

        public override int GetHashCode(TeamInTournament obj)
        {
            return base.GetHashCode();
        }
    }
}

