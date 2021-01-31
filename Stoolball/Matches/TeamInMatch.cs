using System;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class TeamInMatch
    {
        public Guid? MatchTeamId { get; set; }

        public Team Team { get; set; }

        public TeamRole TeamRole { get; set; }

        public bool? WonToss { get; set; }
        public bool? BattedFirst { get; set; }
        public string PlayingAsTeamName { get; set; }
    }
}
