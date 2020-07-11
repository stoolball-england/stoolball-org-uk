using Stoolball.Teams;
using System;

namespace Stoolball.Matches
{
    public class TeamInMatch
    {
        public Guid? MatchTeamId { get; set; }

        public Team Team { get; set; }

        public TeamRole TeamRole { get; set; }

        public bool? WonToss { get; set; }
    }
}
