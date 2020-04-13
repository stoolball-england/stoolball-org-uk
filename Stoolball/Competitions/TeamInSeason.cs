using Stoolball.Teams;
using System;

namespace Stoolball.Competitions
{
    public class TeamInSeason
    {
        public Season Season { get; set; }
        public Team Team { get; set; }
        public DateTimeOffset? WithdrawnDate { get; set; }
    }
}