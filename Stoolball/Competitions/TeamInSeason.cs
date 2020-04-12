using Stoolball.Teams;
using System;

namespace Stoolball.Competitions
{
    public class TeamInSeason
    {
        public Team Team { get; set; }
        public DateTimeOffset? WithdrawnDate { get; set; }
    }
}