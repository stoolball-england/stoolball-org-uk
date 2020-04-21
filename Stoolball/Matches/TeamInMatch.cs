using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class TeamInMatch
    {
        public Team Team { get; set; }

        public TeamRole TeamRole { get; set; }

        public bool? WonToss { get; set; }
    }
}
